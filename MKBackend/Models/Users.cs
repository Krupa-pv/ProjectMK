namespace MKBackend.Models;
using Newtonsoft.Json;

//creates an user model
public class User
    {
        //this is VERY IMPORTANT to ensure the Id property in user model is correctly serialized as id
        [JsonProperty("id")]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Age { get; set; }
        public string Grade { get; set; }
        public string ReadingLevel { get; set;}
        public string Interests { get; set;}

        // user speaking data
    public List<PronunciationFeedback> SpeechFeedback { get; set; } = new List<PronunciationFeedback>();
    public List<TroubleWord> TroubleWords { get; set; } = new List<TroubleWord>();    
    //configure later when time
    //public DateTime LastSpeechPracticeDate { get; set; }
    

   
}




