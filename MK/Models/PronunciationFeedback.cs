namespace MK.Models
{
   public class PronunciationFeedback
{
    public string Text { get; set; }
    public double AccuracyScore { get; set; }
    public double FluencyScore { get; set; }
    public double CompletenessScore { get; set; }
    //public List<TroubleWord> TroubleWords { get; set; } = new List<TroubleWord>();
    public DateTime Timestamp { get; set; }
}

}
