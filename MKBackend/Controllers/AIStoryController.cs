
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using MKBackend.Services;
using MKBackend.Models;
using ModelsUser = MKBackend.Models.User;
[ApiController]
[Route("api/[controller]")]

public class AIStoryController : ControllerBase
{
    private readonly OpenAIClient _oaiclient;

    public AIStoryController (OpenAIClient oaiclient)
    {
        _oaiclient = oaiclient;
        
    }
    

    [HttpPost("generate_speak")]
    public async Task<IActionResult> GenerateSpeechStory([FromBody] SpeechStoryRequest request)
    {
        int length = request.Length switch
        {
            "short" => 100,
            "medium" => 300,
            "long" => 500,
            _ => 300
        };

        var pronunciationData = request.PronunciationData != null && request.PronunciationData.Any();
        
        /*var pronunciationData = request.PronunciationData != null && request.PronunciationData.Any()
        ? string.Join(", ", request.PronunciationData.Select(p => $"{string.Join(", ", p.TroubleWords)} (Accuracy: {p.AccuracyScore}, Fluency: {p.FluencyScore})"))
        : "none";*/

    /*// Prepare trouble words for the prompt
    var troubleWords = request.TroubleWords != null && request.TroubleWords.Any()
        ? string.Join(", ", request.TroubleWords.Select(w => w.Word))
        : "none";
        */

    // Construct the AI prompt
    var prompt = $@"
        Write an educational short story for a student in grade {request.Grade}.
        This student has a {request.ReadingLevel} reading level and interests in {request.Interests}.
        The student struggles with the following pronunciation data: {pronunciationData}.
        The story should be {request.Difficulty} in difficulty and {length} words in length.";

        //Additionally, include the following trouble words frequently: {troubleWords}.

        var chatCompletionsOptions = new ChatCompletionsOptions
        {
            MaxTokens = 8000,
            Temperature = 0.7f,
            FrequencyPenalty = 0.0f,
            PresencePenalty = 0.0f
        };

        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, "You are a speaking tutor specializing in improving verbal skills such as pronunciation."));
        chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, prompt));

        try
        {
            var completionResponse = await _oaiclient.GetChatCompletionsAsync("gpt-4o", chatCompletionsOptions);
            var story = completionResponse.Value.Choices[0].Message.Content;

            if (!string.IsNullOrWhiteSpace(story))
            {
                // Return the generated story as both the story and reference text
                return Ok(new { Story = story, ReferenceText = story });
            }
            else
            {
                return BadRequest(new { Error = "Error! No story was generated." });
            }
        }
        catch (RequestFailedException e)
        {
            return BadRequest(new { Error = e.Message });
        }
    }

   [HttpPost("generate_word")]
    public async Task<IActionResult> GenerateWord([FromBody] WordRequest request)
    {
        try
        {// implement averaged speech feedback later on
            
            // Construct the AI prompt
            var prompt = string.Empty;

            if (request.TroubleWords != null && request.TroubleWords.Any())
            {
                prompt = $@"
                    Generate practice words for a student in grade {request.Grade}.
                    The student struggles with the following trouble words: {string.Join(", ", request.TroubleWords)}.
                    Student has a {request.ReadingLevel} reading level and interests in {request.Interests}
                    Words should be appropriate for the student's age and designed to improve verbal skills and pronunciation.
                    Student wants to attempt words that are {request.Difficulty} for their current level.
                    Include words of varying lengths to challenge the student appropriately. Inlude only the list of words.";
            }
            else
            {
                prompt = $@"
                    Generate practice words for a student in grade {request.Grade}.
                    The student has a {request.ReadingLevel} reading level and interests in {request.Interests}.
                    Words should be appropriate for the student's age and designed to improve verbal skills and pronunciation.
                    Student wants to attempt words that are {request.Difficulty} for their current level. 
                    Include words of varying lengths to challenge the student appropriately. Inlude only the list of words. ";
            }

            // Setup options for Chat Completions
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                MaxTokens = 8000,
                Temperature = 0.7f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f
            };

            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, "You are a speaking tutor specializing in improving verbal skills such as pronunciation by generating words."));
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, prompt));

            // Generate words
            var completionResponse = await _oaiclient.GetChatCompletionsAsync("gpt-4o", chatCompletionsOptions);
            var generatedWords = completionResponse.Value.Choices[0].Message.Content;

            if (!string.IsNullOrWhiteSpace(generatedWords))
            {
                return Ok(new { Words = generatedWords });
            }
            else
            {
                return BadRequest(new { Error = "Error! No words were generated." });
            }
        }
        catch (RequestFailedException e)
        {
            return BadRequest(new { Error = e.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }


    
    public class StoryRequest{
        public string Age {get; set; }
        public string Grade {get; set; }
        public string ReadingLevel {get; set; }
        public string Interests {get; set; }
        public string Difficulty {get; set; } //easy, medium, hard
        public string Length {get; set; } 
        public List<PronunciationFeedback> PronunciationData { get; set; }

        //publoc string TroubleWords {get; set;}
    }

    public class SpeechStoryRequest{
        public string Grade { get; set; }
    public string ReadingLevel { get; set; }
    public string Interests { get; set; }
    public string Difficulty { get; set; }
    public string Length { get; set; } 
    //public List<TroubleWord> TroubleWords { get; set; } = new List<TroubleWord>();
    public List<PronunciationFeedback> PronunciationData { get; set; } = new List<PronunciationFeedback>();
    }
    public class ReadStoryRequest{
        public string Age {get; set; }
        public string Grade {get; set; }
        public string ReadingLevel {get; set; }
        public string Interests {get; set; }
        public string Difficulty {get; set; } //easy, medium, hard
        public string Length {get; set; } 

        //publoc string TroubleWords {get; set;}
    }

    public class WordRequest{
        public string UserId { get; set; } 
        public string Grade { get; set; } 
        public string ReadingLevel { get; set; } 
        public string Interests { get; set; } 
        public string Difficulty { get; set; } 
        public List<string> TroubleWords { get; set; } // The trouble words of the user determined by wordspeak
    }

}
    
 