using System;

namespace MK.Models;

public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Age { get; set; }
    public string Grade { get; set; }
    public string ReadingLevel { get; set;}
    public string Interests { get; set;}

//User speaking data
        
        public List<TroubleWord> TroubleWords { get; set; } = new List<TroubleWord>();
        public List<PronunciationFeedback> SpeechFeedback { get; set; } = new List<PronunciationFeedback>();
        public List<int> ReadingScores { get; set; } = new List<int>();

        public double AverageAccuracyScore { get; set; }
        public double AverageFluencyScore { get; set; }
        public double AverageCompletenessScore { get; set; }
        public string SpeechPracticeFrequency { get; set; }
        public DateTime LastSpeechPracticeDate { get; set; }

        //User reaading data
}
