using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Identity.Client;
using ModelsUser = MKBackend.Models.User;



namespace MKBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImmersiveReaderController : ControllerBase
    {

        private readonly string TenantId;     // Azure subscription TenantId
        private readonly string ClientId;     // Microsoft Entra ApplicationId
        private readonly string ClientSecret; // Microsoft Entra Application Service Principal password
        private readonly string Subdomain;    // Immersive Reader resource subdomain (resource 'Name' if the resource was created in the Azure portal, or 'CustomSubDomain' option if the resource was created with Azure CLI PowerShell. Check the Azure portal for the subdomain on the Endpoint in the resource Overview page, for example, 'https://[SUBDOMAIN].cognitiveservices.azure.com/')

        private readonly Container _container;

        private IConfidentialClientApplication _confidentialClientApplication;
        private IConfidentialClientApplication ConfidentialClientApplication
        {
            get {
                if (_confidentialClientApplication == null) {
                    _confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(ClientId)
                    .WithClientSecret(ClientSecret)
                    .WithAuthority($"https://login.windows.net/{TenantId}")
                    .Build();
                }

                return _confidentialClientApplication;
            }
        }


        public ImmersiveReaderController(IConfiguration configuration, CosmosClient cosmosClient){

            TenantId = configuration["TenantId"];
            ClientId = configuration["ClientId"];
            ClientSecret = configuration["ClientSecret"];
            Subdomain = configuration["Subdomain"];

            if (string.IsNullOrWhiteSpace(TenantId))
            {
                throw new ArgumentNullException("TenantId is null! Did you add that info to secrets.json?");
            }

            if (string.IsNullOrWhiteSpace(ClientId))
            {
                throw new ArgumentNullException("ClientId is null! Did you add that info to secrets.json?");
            }

            if (string.IsNullOrWhiteSpace(ClientSecret))
            {
                throw new ArgumentNullException("ClientSecret is null! Did you add that info to secrets.json?");
            }

            if (string.IsNullOrWhiteSpace(Subdomain))
            {
                throw new ArgumentNullException("Subdomain is null! Did you add that info to secrets.json?");
            }

            _container = cosmosClient.GetContainer(
                configuration["DatabaseName"],
                configuration["ContainerName"]
            );

        }

        public async Task<string> GetTokenAsync()
        {
            const string resource = "https://cognitiveservices.azure.com/";

            var authResult = await ConfidentialClientApplication.AcquireTokenForClient(
                new[] { $"{resource}/.default" })
                .ExecuteAsync()
                .ConfigureAwait(false);

            return authResult.AccessToken;
        }

        [HttpGet]
        public async Task<JsonResult> GetTokenAndSubdomain()
        {


            try
            {
                string tokenResult = await GetTokenAsync();

                return new JsonResult(new { token = tokenResult, subdomain = Subdomain });
            }
            catch (Exception e)
            {
                string message = "Unable to acquire Microsoft Entra token. Check the console for more information.";
                Console.WriteLine(message, e);
                return new JsonResult(new { error = message });
            }
        }

        [HttpPost("saveScore")]
        public async Task<IActionResult> SaveScore ([FromBody] SaveScoreRequest request)
        {
            string userId = request.UserId;
            int score = request.Score;
            try
            {
                Console.WriteLine(score);
                var userResponse = await _container.ReadItemAsync<ModelsUser>(userId, new PartitionKey(userId));
                var user = userResponse.Resource;
                user.ReadingScores ??= new List<int>();
                user.ReadingScores.Add(score);
                List<int> scores = user.ReadingScores;
                int countBadScores = 0;
                int countGoodScores = 0;
                int currentIndex = scores.Count-1;
                int loops = 0;
                bool inRow = true;

                while(currentIndex>=0 && loops<5){
                    if(scores[currentIndex]<4 && loops<3 && inRow){
                        countBadScores++;
                    }
                    else if(scores[currentIndex]>=4){
                        countGoodScores++;
                        inRow = false;
                    }
                    loops++;
                    currentIndex--;
                }
                for(int i = 0; i < scores.Count;i++){
                    Console.WriteLine(scores[i]);
                }
                Console.WriteLine(countBadScores+" bad scores");
                Console.WriteLine(countGoodScores+" good scores");
                List<string> levels = new List<string>();
                levels.Add("Early Reader");
                levels.Add("Intermediate (Simple Chapters)");
                levels.Add("Advanced (Full Chapter Books)");
                levels.Add("Young Adult (Complex Plots)");
                if(countBadScores>=3){
                    string readingLevel = user.ReadingLevel;
                    Console.WriteLine("old level: "+readingLevel);
                    int index = levels.IndexOf(readingLevel);
                    if(index!=0){
                        index--;
                    }
                    Console.WriteLine("new level: "+levels[index]);
                    user.ReadingLevel = levels[index];
                }
                else if(countGoodScores>=5){
                    string readingLevel = user.ReadingLevel;
                    Console.WriteLine("old level: "+readingLevel);
                    int index = levels.IndexOf(readingLevel);
                    if(index!=3){
                        index++;
                    }
                    Console.WriteLine("new level: "+levels[index]);
                    user.ReadingLevel = levels[index];                
                }

                await _container.ReplaceItemAsync(user, userId, new PartitionKey(userId));

                return Ok(new { message = "Feedback saved successfully!" });

            }
            catch (CosmosException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


    }
}

public class SaveScoreRequest{
        public string UserId {get; set; }
        public int Score {get; set; }
    }