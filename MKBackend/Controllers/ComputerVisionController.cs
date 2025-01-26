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
        public static List<BoundingBoxResult> boxes = new List<BoundingBoxResult>();

        public ComputerVisionController(Microsoft.Extensions.Configuration.IConfiguration configuration){

            string endpoint = configuration["CVEndpoint"];
            string key = configuration["CVKey"];


            // Create an Image Analysis client.
            client = new ImageAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));

        }

        [HttpGet("boxes")]
        public async Task<IActionResult> GetBoundingBoxes()
        {
            Console.WriteLine("so the get call to backend is initiated");
            try{
                if (boxes == null || !boxes.Any())
                        {
                            Console.WriteLine("No bounding boxes found.");
                            return NotFound(new { Error = "No bounding boxes available." });
                        }

                        // Log the contents of the bounding boxes (just to check if the data is correct)
                        foreach (var box in boxes)
                        {
                            Console.WriteLine($"Label: {box.Label}, Left: {box.Left}, Top: {box.Top}, Width: {box.Width}, Height: {box.Height}");
                        }                
                return Ok(boxes);
            }
            catch(Exception ex){

                return NotFound(new { Error = ex.Message });
            }

        }

        [HttpPost("analyze")]
        public async Task<IActionResult> analyzeDefaultImage(IFormFile file){

            boxes.Clear();
            if (file == null || file.Length == 0)
            {
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

                        Console.WriteLine($"Object: '{detectedObject.Tags.First().Name}', Bounding box: {detectedObject.BoundingBox}");
                        var boundingBox = new BoundingBoxResult
                        {
                            Label = detectedObject.Tags.First().Name,
                            Left = detectedObject.BoundingBox.X,
                            Top = detectedObject.BoundingBox.Y,
                            Width = detectedObject.BoundingBox.Width,
                            Height = detectedObject.BoundingBox.Height,
                            ImageWidth = result.Metadata.Width,
                            ImageHeight = result.Metadata.Height
                        };
                        boxes.Add(boundingBox);
                    }
                    return Ok(boxes);
            }
 
            }
            catch(Exception ex){

                Console.WriteLine($"Exception caught: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return BadRequest(new { error = ex.Message });

            }

        }
        
    }

    public class BoundingBoxResult
    {
        public string Label { get; set; }
        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float ImageWidth { get; set; }
        public float ImageHeight { get; set; }

    }


}
