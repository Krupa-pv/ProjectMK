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
    
    private string _currentWord;
    private List<string> _generatedWords;
    private AttemptTracker _attemptTracker = new AttemptTracker(); // Track attempts for the current word

    private int _currentWordIndex;        // Index of the current word in the list
    private bool _isSessionActive; 
    private int _attemptCount;
    private readonly string _speechKey = "6p0SyYUApqwf9BSJ6MrqvuGP08yWOuig18Kc8cfX0btiAnd7dQm8JQQJ99BAACYeBjFXJ3w3AAAYACOGYUIR"; // Replace with your Azure Speech Key
    private readonly string _speechRegion = "eastus";  


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
        _attemptTracker.AttemptCount = 1;
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
    if (_attemptTracker.AttemptCount > 3)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FeedbackLabel.Text = "You've already exceeded the maximum of 3 attempts. Please generate a new word.";
        });
        Debug.WriteLine("Exceeded maximum attempts. Cannot proceed.");
        return;
    }

    try
    {
        Debug.WriteLine("Start Recording button clicked.");

        MainThread.BeginInvokeOnMainThread(() =>
        {
            StartRecordingButton.IsEnabled = false; // Disable button during recognition
        });

        // Ensure microphone permissions
        var isAuthorized = await _speechToText.RequestPermissions();
        if (!isAuthorized)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FeedbackLabel.Text = "Microphone access is required for speech recognition.";
            });
            Debug.WriteLine("Microphone access denied.");
            return;
        }

        // Start recognition and validate the word
        var success = await _speechToText.ListenAndAssessAsync(
            _currentWord,
            CultureInfo.GetCultureInfo("en-US"),
            feedback =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FeedbackLabel.Text = feedback; // Dynamically update feedback label
                });
                Debug.WriteLine($"Feedback Updated: {feedback}"); // Log feedback for debugging
            },
            isEnabled =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StartRecordingButton.IsEnabled = isEnabled; // Re-enable button after processing
                });
            },
            CancellationToken.None,
            _attemptTracker);

        if (success)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FeedbackLabel.Text = "Correct! Moving to the next word.";
            });
            Debug.WriteLine("Correct word recognized. Moving to the next word.");
            await MoveToNextWordAsync(_attemptTracker); // Reset tracker for the next word
        }
        else if (_attemptTracker.AttemptCount > 3)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FeedbackLabel.Text = "Maximum attempts reached. Moving to the next word.";
            });
            Debug.WriteLine("Exceeded maximum attempts. Moving to the next word.");
            await SaveTroubleWordAsync(_currentWord);
            await MoveToNextWordAsync(_attemptTracker);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FeedbackLabel.Text = $"Incorrect. Attempt {_attemptTracker.AttemptCount - 1} of 3. Try again!";
            });
            Debug.WriteLine($"Incorrect attempt {_attemptTracker.AttemptCount - 1}.");
        }
    }
    catch (Exception ex)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            FeedbackLabel.Text = $"An error occurred: {ex.Message}";
        });
        Debug.WriteLine($"Error in OnStartRecordingClicked: {ex.Message}");
    }
    finally
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StartRecordingButton.IsEnabled = true; // Ensure button is enabled after processing
        });
        Debug.WriteLine("Finished processing OnStartRecordingClicked.");
    }
}





private async Task SaveTroubleWordAsync(string word)
{
    var troubleWord = new TroubleWord
    {
        Word = word,
        Frequency = 1,
        LastEncountered = DateTime.UtcNow
    };

    Debug.WriteLine($"Sending trouble word for userId: {_userId}");
    Debug.WriteLine($"TroubleWord Data: {JsonSerializer.Serialize(troubleWord)}");

    var success = await _apiService.UpdateTroubleWordAsync(_userId, troubleWord);
    if (success)
    {
        Debug.WriteLine("Trouble word saved successfully.");
    }
    else
    {
        Debug.WriteLine("Error saving trouble word.");
    }
}


private async Task MoveToNextWordAsync(AttemptTracker attemptTracker)
{
    attemptTracker.AttemptCount = 1;
    _currentWordIndex++;

    if (_currentWordIndex < _generatedWords.Count)
    {
        _currentWord = _generatedWords[_currentWordIndex];
        WordLabel.Text = $"Practice this word: {_currentWord}";
        FeedbackLabel.Text = "Click Start Recording to begin practicing!";
        StartRecordingButton.IsEnabled = true; // Enable button for new word
    }
    else
    {
        _isSessionActive = false;
        WordLabel.Text = "All words completed!";
        FeedbackLabel.Text = "Great job! You've completed all the words.";
        StartRecordingButton.IsEnabled = false; // Disable button after completion
    }

    Debug.WriteLine($"Transitioned to word index: {_currentWordIndex}");
}




private void OnStopRecordingClicked(object sender, EventArgs e)
{
    if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
    {
        // Cancel the ongoing recording
        _cancellationTokenSource.Cancel();
        Debug.WriteLine("Recording stopped by the user.");
    }

    // Reset session state
    _isSessionActive = false;
    StartRecordingButton.IsEnabled = true;
    FeedbackLabel.Text = "Recording stopped. You can start again or generate new words.";
    WordLabel.Text = "Ready to start again.";
}





}

