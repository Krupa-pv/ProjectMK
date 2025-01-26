using System.Globalization;
using System.Threading;
using Microsoft.Maui.Controls;
using MK.Services;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.VisualBasic;
using MK.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using System.Text.Json;
using LukeMauiFilePicker;


namespace MK;

public partial class SpeechPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly ISpeechToText _speechToText;
    private CancellationTokenSource _cancellationTokenSource;
    private string _userId;



    public SpeechPage(ApiService apiService, ISpeechToText speechToText)
    {
        InitializeComponent();
        _apiService = apiService;
        _speechToText = speechToText;
        _cancellationTokenSource = new CancellationTokenSource();

        _userId = Preferences.Get("UserId", string.Empty);
        Debug.WriteLine($"Loaded UserId: {_userId}");

        if (string.IsNullOrEmpty(_userId))
        {
            Debug.WriteLine("Error: UserId is not set.");
        }
    }

    // Generate Story
    private async void OnGenerateStoryClicked(object sender, EventArgs e)
    {
        var difficulty = DifficultyPicker.SelectedItem?.ToString();
        var length = LengthPicker.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(difficulty) || string.IsNullOrEmpty(length))
        {
            await DisplayAlert("Error", "Please select both difficulty and length.", "OK");
            return;
        }

        try
        {
            var pronunciationData = await _apiService.GetPronunciationDataAsync(_userId);
            var response = await _apiService.GenerateSpeechStoryAsync(_userId, difficulty, length, pronunciationData);

            StoryLabel.Text = response ?? "No story generated.";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error generating story: {ex.Message}");
            await DisplayAlert("Error", "Failed to generate story. Please try again.", "OK");
        }
    }

    // Start Recording
    private async void OnStartRecordingClicked(object sender, EventArgs e)
{
    if (string.IsNullOrWhiteSpace(StoryLabel.Text) || StoryLabel.Text == "No story generated.")
    {
        await DisplayAlert("Error", "Please generate a story first.", "OK");
        return;
    }

    var isAuthorized = await _speechToText.RequestPermissions();
    if (!isAuthorized)
    {
        await DisplayAlert("Permission Error", "Microphone access is required for speech recognition.", "OK");
        return;
    }

    StartRecordingButton.IsEnabled = false;
    StopRecordingButton.IsEnabled = true;

    try
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var referenceText = StoryLabel.Text;

        // Perform pronunciation assessment
        var assessmentResult = await _speechToText.AssessPronunciation(
            culture,
            referenceText,
            new Progress<string>(partialText =>
            {
                FeedbackLabel.Text = $"Listening: {partialText}";
                Debug.WriteLine($"Microphone Input Recognized: {partialText}");
            }),
            _cancellationTokenSource.Token);

        if (assessmentResult != null)
        {
            // Directly use PronunciationAssessmentWordResult
            //var troubleWords = ExtractTroubleWords(assessmentResult.Words);

            var feedback = new PronunciationFeedback
            {
                Text = referenceText,
                AccuracyScore = assessmentResult.AccuracyScore,
                FluencyScore = assessmentResult.FluencyScore,
                CompletenessScore = assessmentResult.CompletenessScore,
                //TroubleWords = troubleWords,
                Timestamp = DateTime.UtcNow
            };

            
            var success = await _apiService.SaveSpeechFeedbackAsync(_userId, feedback);
            Debug.WriteLine(success ? "Feedback saved successfully!" : "Failed to save feedback.");

            //FeedbackLabel.Text = "Recording complete. Feedback saved.";
            FeedbackLabel.Text = $"Your Accuracy: {feedback.AccuracyScore} \nYour Fluency: {feedback.FluencyScore}";

        }
        else
        {
            FeedbackLabel.Text = "No speech recognized.";
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error during pronunciation assessment: {ex.Message}");
        await DisplayAlert("Error", ex.Message, "OK");
    }
    finally
    {
        StartRecordingButton.IsEnabled = true;
        StopRecordingButton.IsEnabled = false;
    }
}


//extracting toruble words, commented out for now since extracting words from pargraphs is tricky
/*private List<TroubleWord> ExtractTroubleWords(IEnumerable<PronunciationAssessmentWordResult> wordResults)
{
    var troubleWords = new List<TroubleWord>();

    foreach (var wordResult in wordResults)
    {
        if (wordResult.AccuracyScore < 70) // Threshold for trouble words
        {
            troubleWords.Add(new TroubleWord
            {
                Word = wordResult.Word,
                Frequency = 1, // Initialize with 1 for this session
                LastEncountered = DateTime.UtcNow
            });
        }
    }

    return troubleWords;
}*/





private void OnStopRecordingClicked(object sender, EventArgs e)
{
    _cancellationTokenSource?.Cancel();
}





}

