using Microsoft.AspNetCore.Mvc; // For ApiController, HttpPost, ControllerBase, IActionResult
using Microsoft.Azure.Cosmos; // For CosmosClient, Container, PartitionKey
using Microsoft.Extensions.Configuration; // For IConfiguration
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO; // For MemoryStream
using System.Linq; // For LINQ methods like Average
using System.Collections.Generic; // For List<T>
using ModelsUser = MKBackend.Models.User;
using MKBackend.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;


namespace MKBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpeechAssessController : ControllerBase
    {
        private readonly Container _container;
        private IConfiguration _configuration;

        public SpeechAssessController(IConfiguration configuration, CosmosClient cosmosClient)
        {
            _configuration = configuration;
            string databaseName = configuration["DatabaseName"];
            string containerName = configuration["ContainerName"];
            _container = cosmosClient.GetContainer(databaseName,containerName);
        }

        // Get pronunciation data for a user
        [HttpGet("{userId}/pronunciation-data")]
        public async Task<IActionResult> GetPronunciationData(string userId)
        {
            try
            {
                var userResponse = await _container.ReadItemAsync<ModelsUser>(userId, new PartitionKey(userId));
                var user = userResponse.Resource;

                return Ok(user.SpeechFeedback ?? new List<PronunciationFeedback>());
            }
            catch (CosmosException ex)
            {
                return NotFound(new { Error = ex.Message });
                Console.WriteLine("Reached GetPronunciation Data Error");
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> ReturnSpeechInfo()
        {
            try
            {
                string _speechKey = _configuration["_speechKey"];
                string _speechRegion = _configuration["_speechRegion"];

                return Ok(new
                {
                    SpeechKey = _speechKey,
                    SpeechRegion = _speechRegion
                });

            }
            catch (Exception ex)
            {
               return StatusCode(500, new
                {
                    Message = "An unexpected error occurred",
                    Details = ex.Message
                });
            }
        }

        [HttpPut("{userId}/update")]
        public async Task<IActionResult> UpdateTroubleWord(string userId, [FromBody] TroubleWord troubleWord)
        {
            try
            {
                Console.WriteLine($"Received trouble word for userId: {userId}");
                Console.WriteLine($"TroubleWord Data: {JsonSerializer.Serialize(troubleWord)}");

                var userResponse = await _container.ReadItemAsync<ModelsUser>(userId, new PartitionKey(userId));
                var user = userResponse.Resource;


                // Checking if trouble word exists already 
                var existingWord = user.TroubleWords?.FirstOrDefault(w => w.Word == troubleWord.Word);
                if (existingWord != null)
                {
                    // Update frequency and last encountered date
                    existingWord.Frequency += troubleWord.Frequency;
                    existingWord.LastEncountered = troubleWord.LastEncountered;
                }
                else
                {
                    // Add the new trouble word
                    user.TroubleWords ??= new List<TroubleWord>();
                    user.TroubleWords.Add(troubleWord);
                }

                // Save the updated user back to Cosmos DB
                await _container.ReplaceItemAsync(user, userId, new PartitionKey(userId));

                Console.WriteLine("Trouble word saved/updated successfully.");
                return Ok(new { message = "Trouble word saved/updated successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in SaveTroubleWord: {ex.Message}");
                return BadRequest(new { error = ex.Message });
            }
        }


        // Save pronunciation feedback for a user
        [HttpPost("{userId}/ss_savefeedback")]
public async Task<IActionResult> SS_SaveFeedback(string userId, [FromBody] PronunciationFeedback feedback)
{
    try
    {
        // Retrieve the user from Cosmos DB
        Console.WriteLine($"Fetching user with ID: {userId} from Cosmos DB...");
        var userResponse = await _container.ReadItemAsync<ModelsUser>(userId, new PartitionKey(userId));
        var user = userResponse.Resource;

        // Add new feedback
        user.SpeechFeedback ??= new List<PronunciationFeedback>();
        user.SpeechFeedback.Add(feedback);

        
        // Save updated user data in Cosmos DB
        await _container.ReplaceItemAsync(user, userId, new PartitionKey(userId));

        return Ok(new { message = "Feedback and trouble words saved successfully!" });
    }
    catch (CosmosException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}

    }
}
    