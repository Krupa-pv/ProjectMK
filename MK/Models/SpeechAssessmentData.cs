namespace MK.Models;

public class SpeechAssessmentData
{
    public List<PronunciationFeedback> Feedback { get; set; } = new List<PronunciationFeedback>();
    //public List<TroubleWord> TroubleWords { get; set; } = new List<TroubleWord>();
}