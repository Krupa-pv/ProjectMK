using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Azure.AI.Vision.ImageAnalysis;
using System;
using System.IO;
using Microsoft.Identity.Client;
using Microsoft.Azure.Cosmos;
using ModelsUser = MKBackend.Models.User;
using Azure;

namespace MKBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class ComputerVisionController : ControllerBase
    {

        ImageAnalysisClient client;

public ComputerVisionController(Microsoft.Extensions.Configuration.IConfiguration configuration){

            string endpoint = configuration["CVEndpoint"];
            string key = configuration["CVKey"];


            //string endpoint = "https://elexircv.cognitiveservices.azure.com/";
            //string key = "9ZwSLUi6C4JFj77KNdfJY5sjdHG4xXoAIWxrbJdNcSONGfcoe79hJQQJ99BAACYeBjFXJ3w3AAAFACOGsVNQ";

            // Create an Image Analysis client.
            client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));

        }

        [HttpPost("analyze")]
        public async Task<IActionResult> analyzeDefaultImage(IFormFile file){

            Console.WriteLine("boi we in da backkkk");
            if (file == null || file.Length == 0)
            {
                Console.WriteLine("the fle is null....");
                return BadRequest(new { error = "No file uploaded." });
            }

            try{
            using (var stream = file.OpenReadStream())
            {    

                // Get the tags for the image.
                ImageAnalysisResult result = client.Analyze(
                    BinaryData.FromStream(stream),
                    VisualFeatures.Objects);
                    

                // Print object detection results to the console
                Console.WriteLine($"Image analysis results:");
                Console.WriteLine($" Metadata: Model: {result.ModelVersion} Image dimensions: {result.Metadata.Width} x {result.Metadata.Height}");
                // Check for each object and ensure it's not null before accessing properties
                    foreach (var detectedObject in result.Objects.Values)
                    {
                        if (detectedObject == null)
                        {
                            Console.WriteLine("Detected object is null.");
                            continue; // Skip this object and move to the next one
                        }

                        if (detectedObject.Tags == null || !detectedObject.Tags.Any())
                        {
                            Console.WriteLine("Detected object has no tags.");
                            continue;
                        }

                        Console.WriteLine($"Object: '{detectedObject.Tags.First().Name}', Bounding box: {detectedObject.BoundingBox}");
                    }
                    return Ok(new { message = "Feedback saved successfully!" });
            }
 
            }
            catch(Exception ex){

                Console.WriteLine($"Exception caught: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return BadRequest(new { error = ex.Message });

            }

        }
        
    }
}
