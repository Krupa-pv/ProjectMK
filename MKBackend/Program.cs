using MKBackend.Services;
using Microsoft.EntityFrameworkCore;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Azure.Cosmos;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.


builder.Services.AddControllers();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string connectionString = builder.Configuration["ConnectionString"];
Console.WriteLine($"Cosmos DB Connection String: {connectionString}");

string databaseName = builder.Configuration["DatabaseName"];
string containerName = builder.Configuration["ContainerName"];


// Add Cosmos DB service
//registering cosmos with DI container
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    Console.WriteLine("Initializing CosmosClient");
    return new CosmosClient(connectionString); // Creating CosmosClient
});

// Register CosmosDBService as a Singleton (it now takes CosmosClient in the constructor)
builder.Services.AddSingleton<CosmosDBService>(sp =>
{
    var cosmosClient = sp.GetRequiredService<CosmosClient>();
    return new CosmosDBService(cosmosClient, databaseName, containerName);
});

//add open ai service with DI container


string openAIUrl = builder.Configuration["OpenAIURL"];
string keyCred = builder.Configuration["OpenAIKeyCred"];
builder.Services.AddSingleton(new OpenAIClient(
    new Uri(openAIUrl),
    new Azure.AzureKeyCredential(keyCred)
));

var app = builder.Build();



// Enables Swagger in Development mode
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthorization();
app.MapControllers();
app.Run();

