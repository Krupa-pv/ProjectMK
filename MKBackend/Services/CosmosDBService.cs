using System;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;
using System.Collections.Generic;
using ModelsUser = MKBackend.Models.User;
using Newtonsoft.Json;
//using MKBackend.Controllers;

namespace MKBackend.Services;


    public class CosmosDBService
    {
        private readonly Container _container;

            
        // this is the constructor
        public CosmosDBService(CosmosClient cosmosClient, string databaseName, string containerName)
        {
            Console.WriteLine("Initializing CosmosDBService");
            _container = cosmosClient.GetContainer(databaseName, containerName);
            
        }

        // to add or update user

        public async Task AddUpdateUserAsync(ModelsUser user)
        {
            Console.WriteLine($"Upserting item: {JsonConvert.SerializeObject(user)}");
            try
            {
                await _container.UpsertItemAsync(user, new PartitionKey(user.Id));
            }
            catch (CosmosException ex)
            {
                Console.WriteLine($"Cosmos DB error: {ex.StatusCode}, {ex.Message}");
                throw;
            }

        }
        

        // retrieve a user by ID
        public async Task<ModelsUser?> GetUserByIdAsync(string userId)
            {
                try
                {
                    var response = await _container.ReadItemAsync<ModelsUser>(
                        userId, 
                        new PartitionKey(userId));
                    return response.Resource;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null; // User not found
                }
            }   


    }
    

