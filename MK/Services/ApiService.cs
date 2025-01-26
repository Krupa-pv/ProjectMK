using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using ModelsUser = MK.Models.User;
using MK.Models;
using System.Diagnostics;
using System.Net.Http.Headers; // For MediaTypeHeaderValue
using LukeMauiFilePicker;




namespace MK.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;

    static readonly Dictionary<DevicePlatform, IEnumerable<string>> FileType = new()
    {
        {  DevicePlatform.Android, new[] { "text/*" } } ,
        { DevicePlatform.iOS, new[] { "public.json", "public.plain-text" } },
        { DevicePlatform.MacCatalyst, new[] { "public.jpg", "public.png" } },
        { DevicePlatform.WinUI, new[] { ".txt", ".json" } }
    };

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5100/") // make sure to use live backend URL
        };

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    // Add user
    public async Task<bool> AddUserAsync(ModelsUser user)
    {
        var json = JsonSerializer.Serialize(user, _serializerOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/users", content);
        return response.IsSuccessStatusCode;
    }

    // Get user by ID
    public async Task<ModelsUser> GetUserAsync(string id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/users/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ModelsUser>(content, _serializerOptions);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    //generates a story based on user preferences 
       public async Task<string> GenerateStoryAsync(string userId, string difficulty, string length)
    {
        // Fetch user info
        var userInfo = await GetUserAsync(userId);
        if (userInfo == null) return "User not found.";

        var storyRequest = new
        {
            Age = userInfo.Age.ToString(),
            Grade = userInfo.Grade,
            ReadingLevel = userInfo.ReadingLevel,
            Interests = userInfo.Interests,
            //TroubleWords = userInfo.TroubleWords,
            Difficulty = difficulty,
            Length = length
            
        };

        var response = await _httpClient.PostAsJsonAsync("api/aistory/generate_read", storyRequest);

        if (response.IsSuccessStatusCode)
        {
            // Deserialize into the strongly typed StoryResponse class
            var result = await response.Content.ReadFromJsonAsync<StoryResponse>();
            return result?.Story ?? "No story generated.";  // Return the story from the response
        }
        else
        {
            return "Error generating story.";
        }
    }

    //generating speech story
    public async Task<string> GenerateSpeechStoryAsync(string userId, string difficulty, string length, List<PronunciationFeedback> pronunciationData)
    {
        // Fetch user info
        var userInfo = await GetUserAsync(userId);
        if (userInfo == null) return "User not found.";

        var storyRequest = new
        {
            Age = userInfo.Age.ToString(),
            Grade = userInfo.Grade,
            ReadingLevel = userInfo.ReadingLevel,
            Interests = userInfo.Interests,
            //TroubleWords = userInfo.TroubleWords,
            Difficulty = difficulty,
            Length = length,
            PronunciationData = pronunciationData
            
        };

        var response = await _httpClient.PostAsJsonAsync("api/aistory/generate_speak", storyRequest);

        if (response.IsSuccessStatusCode)
        {
            // Deserialize into the strongly typed StoryResponse class
            var result = await response.Content.ReadFromJsonAsync<StoryResponse>();
            return result?.Story ?? "No story generated.";  // Return the story from the response
        }
        else
        {
            return "Error generating story.";
        }
    }


    


    //retrieving prnonunciation data from cosmos 
    public async Task<List<PronunciationFeedback>> GetPronunciationDataAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/speechassess/{userId}/pronunciation-data");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<PronunciationFeedback>>() ?? new List<PronunciationFeedback>();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error fetching pronunciation data: {ex.Message}");
        }

        return new List<PronunciationFeedback>();
    }

    

    // Save speech feedback to the backend
    public async Task<bool> SaveSpeechFeedbackAsync(string userId, PronunciationFeedback feedback)
    {
        try
        {
            Debug.WriteLine($"Sending feedback for userId: {userId}");
            Debug.WriteLine($"Feedback: {JsonSerializer.Serialize(feedback)}");

            var endpoint = $"api/speechassess/{userId}/ss_savefeedback";
            Debug.WriteLine($"Calling API: {_httpClient.BaseAddress}{endpoint}");

            var response = await _httpClient.PostAsJsonAsync($"api/speechassess/{userId}/ss_savefeedback", feedback);

            if (!response.IsSuccessStatusCode)
            {
                var errorDetails = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Error saving feedback. Status code: {response.StatusCode}");
                Debug.WriteLine($"Error details: {await response.Content.ReadAsStringAsync()}");
            }
            else
            {
                Debug.WriteLine("Feedback saved successfully!");
            }


            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in SaveSpeechFeedbackAsync: {ex.Message}");
            return false;
        }
    }


    //generate words 
    public async Task<string> GenerateWordAsync(string userId, string difficulty)
    {
        try 
        {
            // Fetch user info
            var userInfo = await GetUserAsync(userId);
            if (userInfo == null) return "User not found.";

            var filteredTroubleWords = userInfo.TroubleWords?
            .Where(tw => (DateTime.UtcNow - tw.LastEncountered).TotalDays <= 180) // Last 6 months
            .OrderByDescending(tw => tw.Frequency) // Most frequent first
            .Take(10) // Limit to top 10
            .Select(tw => tw.Word)
            .ToList();

            var wordRequest = new WordRequest
            {
                UserId = userId,
                Grade = userInfo.Grade, // Ensure Grade is an integer
                ReadingLevel = userInfo.ReadingLevel,
                Interests = userInfo.Interests,
                TroubleWords = filteredTroubleWords ?? new List<string>(),
                Difficulty = difficulty
            
            };

            var response = await _httpClient.PostAsJsonAsync("api/aistory/generate_word", wordRequest);

            if (response.IsSuccessStatusCode)
            {
                
                var result = await response.Content.ReadFromJsonAsync<WordResponse>();
                return result?.Words ?? "No words generated.";  // Return the word from the response
            }
            else
            {
                // Handle errors
                var errorDetails = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Error generating word. Status code: {response.StatusCode}. Details: {errorDetails}");
                return "Error generating word.";
            }
        }
        
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GenerateWordAsync: {ex.Message}");
            return "An error occurred while generating the word.";
        }
        
    }

    //for saving or updating info about a trouble word
    public async Task<bool> UpdateTroubleWordAsync(string userId, TroubleWord troubleWord)
    {
        // Fetch user info
        var userInfo = await GetUserAsync(userId);
        if (userInfo == null) 
        {
            Debug.WriteLine("User Not Found!");
            return false;
        }

        try
        {
            Debug.WriteLine($"Sending trouble word for userId: {userId}");
            Debug.WriteLine($"TroubleWord Data: {JsonSerializer.Serialize(troubleWord)}");

            // Send the trouble word to the backend
            var response = await _httpClient.PostAsJsonAsync($"api/words/{userId}/update", troubleWord);

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"Error saving trouble word. Status code: {response.StatusCode}");
                Debug.WriteLine($"Error details: {await response.Content.ReadAsStringAsync()}");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in SaveOrUpdateTroubleWordAsync: {ex.Message}");
            return false;
        }
    }

    public async Task<Tuple<string,string>> GetSpeechInfo ()
    {
        Debug.WriteLine("getting speech info");
        var response  = await _httpClient.GetAsync($"api/SpeechAssess/info");
        var content = "error";
        Tuple<string,string> speechInfo = new Tuple<string, string>("error","error");

        if(response.IsSuccessStatusCode){
            content = await response.Content.ReadAsStringAsync();
            var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(content);
            string speechKey = jsonResponse.key;
            string speechRegion = jsonResponse.region;
            Debug.WriteLine("here and speecehkey is" + speechKey);
            speechInfo = new Tuple<string, string>(speechKey,speechRegion);
        }
        else{
            Debug.WriteLine("failed");
        }
        return speechInfo;
        //deserialize the JSON response
       // var speechJson = await response.Content.ReadAsStringAsync();
        //var speechInfo = JsonSerializer.Deserialize<SpeechInfo>(speechJson);

        //return speechInfo;
 
    }


    public async Task<ImageSource> uploadFileToBackend (IFilePickerService picker){
        try
        {   
            var file = await picker.PickFileAsync("Select a file", FileType); 
            if(file!=null){

                var imageStream = await file.OpenReadAsync();
                var imageSource = ImageSource.FromStream(() => imageStream);


                var fileStream = await file.OpenReadAsync();
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                string contentType = "application/octet-stream"; // Default value

                if (fileExtension == ".jpg" || fileExtension == ".jpeg")
                {
                    contentType = "image/jpeg";
                }
                else if (fileExtension == ".png")
                {
                    contentType = "image/png";
                }
                else if (fileExtension == ".gif")
                {
                    contentType = "image/gif";
                }
                else if (fileExtension == ".bmp")
                {
                    contentType = "image/bmp";
                }
                var content = new MultipartFormDataContent();
                var streamContent = new StreamContent(fileStream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                // Step 5: Add the file to the content (file name must match the backend's parameter name)
                content.Add(streamContent, "file", file.FileName);

                // Step 6: Send the file to the backend via POST
                var response = await _httpClient.PostAsync($"api/computervision/analyze", content);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("File uploaded successfully!");
                    return imageSource;
                }
                else
                {
                    Debug.WriteLine($"Failed to upload file. Status Code: {response.StatusCode}");
                    return null;
                }


            }
            return null;

        }
        catch(Exception ex){
            Debug.WriteLine($"Error: {ex.Message}");
            return null;
        }

    }

    public async Task<List<BoundingBoxResult>> getBoxes(){

        try{

            Debug.WriteLine("came to the api client!");

            var response = await _httpClient.GetAsync($"api/computervision/boxes");


            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<BoundingBoxResult>>();
            }
            return new List<BoundingBoxResult>();


        }
        catch(Exception ex){
            Debug.WriteLine("got caught");
            Debug.WriteLine($"Error: {ex.Message}");
            return new List<BoundingBoxResult>();
        }
    }

    public async Task<Tuple<string,string>> getTokenAndSubdomainAsync(){
        
        Debug.WriteLine("i am here");
        var response = await _httpClient.GetAsync("api/immersivereader");
        var content = "error";
        Tuple<string,string> hi = new Tuple<string, string>("error","error");
        if (response.IsSuccessStatusCode)
            {
                content = await response.Content.ReadAsStringAsync();
                var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(content);
                string token = jsonResponse.token;
                string subdomain = jsonResponse.subdomain;
                hi = new Tuple<string, string>(token,subdomain);

            }
        else{
            Debug.WriteLine("failed");
        }
        return hi;
        

    }


    //class used to map the JSON response
    public class SpeechInfo
        {
            public string SpeechKey { get; set; }
            public string SpeechRegion { get; set; }
        }

}

public class WordRequest{
        public string UserId { get; set; } 
        public string Grade { get; set; } 
        public string ReadingLevel { get; set; } 
        public string Interests { get; set; } 
        public string Difficulty { get; set; } 
        public List<string> TroubleWords { get; set; } // The trouble words of the user determined by wordspeak
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

