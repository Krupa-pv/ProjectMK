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

namespace MK;

public partial class WordSpeakPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly ISpeechToText _speechToText;
    private CancellationTokenSource _cancellationTokenSource;
    private string _userId;
    private int _attemptCount;
    private string _currentWord;
    private List<string> _generatedWords;

    private int _currentWordIndex;        // Index of the current word in the list
    private bool _isSessionActive; 


    public WordSpeakPage(ApiService apiService, ISpeechToText speechToText)
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
        _generatedWords = new List<string>();
        _currentWordIndex = 0;
        _attemptCount = 0;
        _isSessionActive = false;
    }

    // Generate words based on user input
    private async void OnGenerateWordClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("Generating Story...");
        FeedbackLabel.Text = "Generating Word...";
        var difficulty = DifficultyPicker.SelectedItem?.ToString();

        try
        {
            var generatedWords = await _apiService.GenerateWordAsync(_userId, difficulty);
            Debug.WriteLine(generatedWords);
            if (!string.IsNullOrWhiteSpace(generatedWords))
            {
                var cleanedWords = System.Text.RegularExpressions.Regex.Replace(generatedWords, @"\d+\.\s?", "")
                                                                        .Replace("-", "");
            Debug.WriteLine($"Cleaned Words: {cleanedWords}");

            // Split words from the cleaned response
            _generatedWords = cleanedWords.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(w => w.Trim())
                                          .ToList();
            Debug.WriteLine($"Split Words: {string.Join(", ", _generatedWords)}");

            
            _currentWordIndex = 0;
            _isSessionActive = true;

                WordLabel.Text = $"Practice this word: {_generatedWords[_currentWordIndex]}";
                FeedbackLabel.Text = "Click Start Recording when you are ready to practice!";
                _currentWord = _generatedWords[_currentWordIndex];
                StartRecordingButton.IsEnabled = true;
            }
            else
            {
                WordLabel.Text = "No words generated.";
                _generatedWords = new List<string>();
                StartRecordingButton.IsEnabled = false;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error generating words: {ex.Message}");
            await DisplayAlert("Error", "Failed to generate words.", "OK");
            StartRecordingButton.IsEnabled = false;
        }
    }

    
    // Start practicing the current word
    private async void OnStartRecordingClicked(object sender, EventArgs e)
    {
        if (!_isSessionActive || _currentWordIndex >= _generatedWords.Count)
        {
            await DisplayAlert("Error", "No words to practice. Please generate words first.", "OK");
            return;
        }

        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        var isAuthorized = await _speechToText.RequestPermissions();
        if (!isAuthorized)
        {
            await DisplayAlert("Permission Error", "Microphone access is required for speech recognition.", "OK");
            return;
        }

        StartRecordingButton.IsEnabled = false;
        var wordRecognized = false;

        try
        {
            var culture = CultureInfo.GetCultureInfo("en-US");

            var assessmentResult = await _speechToText.DiscreteAssessPronunciation(
                culture,
                _currentWord,
                new Progress<string>(partialText =>
                {
                    Debug.WriteLine($"Microphone Input Recognized: {partialText}");

                }),
                _cancellationTokenSource.Token);
            if (assessmentResult == null)
            {
                Debug.WriteLine("Assessment result is null.");
                return;
            }

            FeedbackLabel.Text = "Assessing Word!";
            Debug.WriteLine($"Assessment Result Details: AccuracyScore={assessmentResult.AccuracyScore}, FluencyScore={assessmentResult.FluencyScore}");


            if (assessmentResult.AccuracyScore >= 80 
            && assessmentResult.FluencyScore >= 80)
            {
                wordRecognized = true;
                Debug.WriteLine("Word recognized with sufficient accuracy and fluency!");

                FeedbackLabel.Text = "Correct! Moving to the next word...";
                await Task.Delay(2000);
                await NextWordAsync();
            }

            else if (assessmentResult != null 
            && assessmentResult.AccuracyScore < 80 
            && assessmentResult.FluencyScore < 80)
            {
                _attemptCount++;
                if (_attemptCount >= 3)
                {
                    var troubleWord = new TroubleWord
                    {
                        Word = _currentWord,
                        Frequency = 1,
                        LastEncountered = DateTime.UtcNow
                    };

                    var success = await _apiService.UpdateTroubleWordAsync(_userId, troubleWord);
                    Debug.WriteLine(success ? "Trouble word saved/updated successfully!" : "Failed to save/update trouble word.");
                    FeedbackLabel.Text = "Let's try this word again later. Moving to the next word.";

                    await Task.Delay(2000);
                    await NextWordAsync();
                }
                else
                {
                    FeedbackLabel.Text = $"Try again! Attempts left: {3 - _attemptCount}";
                    StartRecordingButton.IsEnabled = true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during pronunciation assessment: {ex.Message}");
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            if (_isSessionActive)
            {
                StartRecordingButton.IsEnabled = true;
            }
        }
    }





private async Task NextWordAsync()
    {
        _attemptCount = 0;
        _currentWordIndex++;

        if (_currentWordIndex < _generatedWords.Count)
        {
            _currentWord = _generatedWords[_currentWordIndex];
            WordLabel.Text = $"Practice this word: {_currentWord}";
            StartRecordingButton.IsEnabled = true;
        }
        else
        {
            _isSessionActive = false;
            WordLabel.Text = "All words completed! Generate new words to continue practicing.";
            FeedbackLabel.Text = "Great job! You've completed all the words.";
            StartRecordingButton.IsEnabled = false;
        }

        Debug.WriteLine($"Transitioned to word index: {_currentWordIndex}");
    }

private void OnStopRecordingClicked(object sender, EventArgs e)
{
   //Cancel current recording
    _cancellationTokenSource?.Cancel();

    //Resettting session state
    _isSessionActive = false;
    StartRecordingButton.IsEnabled = true;
    FeedbackLabel.Text = "Recording stopped. You can start again or generate new words.";
    WordLabel.Text = "Ready to start again.";
}




}

