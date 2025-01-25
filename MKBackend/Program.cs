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

var config = builder.Configuration.GetSection("CosmosDB");


string connectionString = config["ConnectionString"] ?? throw new InvalidOperationException("ConnectionString is not configured.");
Console.WriteLine($"Cosmos DB Connection String: {connectionString}");

string databaseName = config["DatabaseName"] ?? throw new InvalidOperationException("DatabaseName is not configured.");
string containerName = config["ContainerName"] ?? throw new InvalidOperationException("ContainerName is not configured.");

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
//builder.Services.AddSingleton(sp => new CosmosDBService(connectionString, databaseName, containerName));

//add open ai service with DI container

//UNCOMMENT THIS

/*builder.Services.AddSingleton(new OpenAIClient(
    new Uri("ADD URL"),
    new Azure.AzureKeyCredential("ADD KEY")
));*/

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

