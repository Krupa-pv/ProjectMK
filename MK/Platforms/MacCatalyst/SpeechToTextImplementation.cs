using System.Globalization;
using System.Threading;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using MK;
using Microsoft.Extensions.Configuration;
using MK.Models;

namespace MK.Platforms.MacCatalyst
{
    public class SpeechToTextImplementation : ISpeechToText
    {

        private readonly IConfiguration Configuration;
        private readonly string _speechKey = "6p0SyYUApqwf9BSJ6MrqvuGP08yWOuig18Kc8cfX0btiAnd7dQm8JQQJ99BAACYeBjFXJ3w3AAAYACOGYUIR"; // Replace with your Azure Speech Key
        private readonly string _speechRegion = "eastus";  
        public string _recognizedWord;

        public SpeechToTextImplementation(IConfiguration configuration){
            /*Configuration = configuration; 
            var _speechKey = Configuration["SpeechService: Key"];
            var _speechRegion = Configuration["SpeechService: Region"];
            Debug.WriteLine($"speechkey: {_speechKey}");
            Debug.WriteLine($"speechkey: {_speechRegion}");*/
            
            
        } 
        public async Task<bool> RequestPermissions()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();

                if (status == PermissionStatus.Granted)
                {
                    Debug.WriteLine("Microphone permission granted.");
                    return true;
                }
                else
                {
                    Debug.WriteLine("Microphone permission denied.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error requesting permissions: {ex.Message}");
                return false;
            }
        }

  
 public async Task<(string RecognizedText, AudioConfig AudioConfig)> Listen(
    CultureInfo culture,
    IProgress<string> recognitionResult,
    CancellationToken cancellationToken)
{
    var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
    speechConfig.SpeechRecognitionLanguage = culture.Name;

    var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
    using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

    var tcs = new TaskCompletionSource<string>();

    recognizer.Recognizing += (s, e) =>
    {
        if (e.Result.Reason == ResultReason.RecognizingSpeech)
        {
            recognitionResult.Report(e.Result.Text);
        }
    };

    recognizer.Recognized += (s, e) =>
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            var recognizedText = e.Result.Text.Replace(".", "").Replace(",", "").Trim();
            //Debug.WriteLine($"Recognized Speech: {recognizedText}");
            tcs.TrySetResult(recognizedText);
        }
        else
        {
            Debug.WriteLine("No match found.");
            tcs.TrySetResult(string.Empty);
        }
    };

    recognizer.Canceled += (s, e) =>
    {
        Debug.WriteLine($"Recognition canceled. Reason: {e.Reason}");
        tcs.TrySetResult(string.Empty);
    };

    try
    {
        await recognizer.StartContinuousRecognitionAsync();
        using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            var recognizedText = await tcs.Task;
            return (recognizedText, audioConfig);
        }
    }
    finally
    {
        await recognizer.StopContinuousRecognitionAsync();
    }
}
    



    /*public async Task<bool> ListenAndAssessAsync(
        string expectedWord,
        CultureInfo culture,
        Action<string> updateFeedback,
        Action<bool> updateButtonState,
        CancellationToken cancellationToken)
    {
        try
        {
            Debug.WriteLine("Initializing speech recognition...");
            Debug.WriteLine($"Expected Word: {expectedWord}");
            Debug.WriteLine($"Culture: {culture.Name}");

            // Configure speech service
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechRecognitionLanguage = culture.Name;
            speechConfig.OutputFormat = OutputFormat.Detailed; // Enable detailed output
            speechConfig.SetProperty("SPEECH-SDK-INITIAL-SILENCE-THRESHOLD-MS", "5000"); // 5 seconds
            speechConfig.SetProperty("SPEECH-SDK-END-OF-SPEECH-TIMEOUT-MS", "2000");     // 2 seconds

            Debug.WriteLine("Speech configuration initialized.");

            // Configure pronunciation assessment
            var pronunciationConfig = new PronunciationAssessmentConfig(
                expectedWord.Trim(),
                GradingSystem.HundredMark,
                Granularity.Phoneme,
                enableMiscue: true);

            Debug.WriteLine("Pronunciation assessment configuration created.");

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            Debug.WriteLine("Audio and speech recognizer initialized.");

            // Apply pronunciation configuration
            pronunciationConfig.ApplyTo(recognizer);
            Debug.WriteLine("Pronunciation configuration applied to recognizer.");

            // Debug partial results
            recognizer.Recognizing += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizingSpeech)
                {
                Debug.WriteLine($"Final Recognized Result: {e.Result.Text}, Confidence: {e.Result.Confidence}");                    updateFeedback($"Listening... Partial: {e.Result.Text}");
                }
            };

            // Debug recognized results
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Debug.WriteLine($"Final Recognized Result: {e.Result.Text}");
                }
            };

            updateFeedback("Listening...");
            Debug.WriteLine("Listening started for pronunciation assessment.");

            // Start recognition
            var result = await recognizer.RecognizeOnceAsync();
            Debug.WriteLine("Recognition completed. Processing result...");

            // Handle result
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                var recognizedText = result.Text.Trim();
                Debug.WriteLine($"Recognized Speech: {recognizedText}");
                Debug.WriteLine($"Matching recognized speech with expected word: {expectedWord}");

                updateFeedback($"Recognized Speech: {recognizedText}");

                // Pronunciation assessment result
                var assessmentResult = PronunciationAssessmentResult.FromResult(result);
                if (assessmentResult != null)
                {
                    Debug.WriteLine("Pronunciation Assessment Results:");
                    Debug.WriteLine($"Accuracy: {assessmentResult.AccuracyScore}");
                    Debug.WriteLine($"Fluency: {assessmentResult.FluencyScore}");
                    Debug.WriteLine($"Completeness: {assessmentResult.CompletenessScore}");

                    updateFeedback($"Accuracy: {assessmentResult.AccuracyScore}, Fluency: {assessmentResult.FluencyScore}, Completeness: {assessmentResult.CompletenessScore}");
                    return true;
                }
                else
                {
                    Debug.WriteLine("Pronunciation assessment result is null.");
                    updateFeedback("Pronunciation assessment failed. Please try again.");
                    return false;
                }
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Debug.WriteLine("No match found for speech recognition.");
                var noMatchDetails = NoMatchDetails.FromResult(result);
                Debug.WriteLine($"NoMatch Details: {noMatchDetails.Reason}");
                updateFeedback("No match found. Please try again.");
                return false;
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                Debug.WriteLine($"Speech recognition canceled: {cancellation.Reason}");
                updateFeedback($"Listening canceled: {cancellation.Reason}");
                return false;
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Speech recognition canceled due to timeout or user cancellation.");
            updateFeedback("Listening timed out. Please try again.");
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error during speech recognition: {ex.Message}");
            updateFeedback($"An error occurred: {ex.Message}");
            return false;
        }
        finally
        {
            updateButtonState(true); // Re-enable the Start Recording button
            Debug.WriteLine("Listening operation completed.");
        }
    }*/

    public async Task<bool> ListenAndAssessAsync(
    string expectedWord,
    CultureInfo culture,
    Action<string> updateFeedback,
    Action<bool> updateButtonState,
    CancellationToken cancellationToken,
    AttemptTracker attemptTracker)
{
    try
    {
        Debug.WriteLine("Initializing basic speech recognition with validation...");
        Debug.WriteLine($"Expected Word: {expectedWord}");
        Debug.WriteLine($"Culture: {culture.Name}");
        Debug.WriteLine($"Attempt Number: {attemptTracker.AttemptCount}");

        // Configure speech service
        var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
        speechConfig.SpeechRecognitionLanguage = culture.Name;
        speechConfig.OutputFormat = OutputFormat.Detailed; // Enable detailed output

        // Increase silence thresholds to allow more time for input
        speechConfig.SetProperty("SPEECH-SDK-INITIAL-SILENCE-THRESHOLD-MS", "5000"); // 5 seconds
        speechConfig.SetProperty("SPEECH-SDK-END-OF-SPEECH-TIMEOUT-MS", "2000");    // 2 seconds

        Debug.WriteLine("Speech configuration initialized.");

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        Debug.WriteLine("Audio and speech recognizer initialized.");

        // Debug partial results
        recognizer.Recognizing += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizingSpeech)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    updateFeedback($"Listening... Partial: {e.Result.Text}"); // Ensure feedback updates on UI
                });
                Debug.WriteLine($"Partial Result: {e.Result.Text}");
            }
        };

        // Debug recognized results
        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                Debug.WriteLine($"Final Recognized Result: {e.Result.Text}");
            }
        };

        MainThread.BeginInvokeOnMainThread(() =>
        {
            updateFeedback($"Listening... Attempt {attemptTracker.AttemptCount}"); // Feedback for current attempt
        });

        Debug.WriteLine("Listening started for basic recognition.");

        // Start recognition
        var result = await recognizer.RecognizeOnceAsync();
        Debug.WriteLine("Recognition completed. Processing result...");

        // Handle result
        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            var recognizedText = result.Text.Trim().Replace(".", "").ToLowerInvariant(); // Normalize recognized text
            Debug.WriteLine($"Recognized Speech: {recognizedText}");

            if (recognizedText == expectedWord.ToLowerInvariant())
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    updateFeedback($"Correct! You succeeded in {attemptTracker.AttemptCount} attempts.");
                });
                return true; // Indicate success
            }
            else
            {
                attemptTracker.AttemptCount++;
                if (attemptTracker.AttemptCount > 3)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        updateFeedback($"Incorrect. You have exceeded the maximum of 3 attempts. Moving to the next word.");
                    });
                    return false; // Indicate failure after 3 attempts
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        updateFeedback($"Incorrect. Attempt {attemptTracker.AttemptCount - 1} of 3. Try again!");
                    });
                    return false; // Indicate mismatch but allow retry
                }
            }
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            attemptTracker.AttemptCount++;
            if (attemptTracker.AttemptCount > 3)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    updateFeedback($"No match found. You have exceeded the maximum of 3 attempts. Moving to the next word.");
                });
                return false;
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    updateFeedback($"No match found. Attempt {attemptTracker.AttemptCount} of 3. Please try again.");
                });
                return false;
            }
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(result);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                updateFeedback($"Listening canceled: {cancellation.Reason}");
            });
            return false;
        }

        return false;
    }
    catch (Exception ex)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            updateFeedback($"An error occurred: {ex.Message}");
        });
        Debug.WriteLine($"Error during speech recognition: {ex.Message}");
        return false;
    }
    finally
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            updateButtonState(true); // Ensure button is re-enabled on UI
        });
        Debug.WriteLine("Listening operation completed.");
    }
}





    
public async Task<PronunciationAssessmentResult> DiscreteAssessPronunciation(
    CultureInfo culture,
    string referenceText,
    AudioConfig audioConfig,
    IProgress<string> recognitionResult,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrEmpty(referenceText))
    {
        Debug.WriteLine("Reference text for pronunciation assessment is null or empty.");
        return null;
    }

    var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
    speechConfig.SpeechRecognitionLanguage = culture.Name;

    var pronunciationConfig = new PronunciationAssessmentConfig(
        referenceText,
        GradingSystem.HundredMark,
        Granularity.Phoneme,
        enableMiscue: true);

    using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

    pronunciationConfig.ApplyTo(recognizer);

    var tcs = new TaskCompletionSource<PronunciationAssessmentResult>();

    recognizer.Recognized += (s, e) =>
    {
        if (e.Result.Reason == ResultReason.RecognizedSpeech)
        {
            Debug.WriteLine($"Recognized Speech for Pronunciation Assessment: {e.Result.Text}");
            recognitionResult.Report(e.Result.Text);

            var assessmentResult = PronunciationAssessmentResult.FromResult(e.Result);
            if (assessmentResult != null)
            {
                Debug.WriteLine($"Assessment Result: Accuracy={assessmentResult.AccuracyScore}, Fluency={assessmentResult.FluencyScore}");
                tcs.TrySetResult(assessmentResult);
            }
        }
    };

    recognizer.Canceled += (s, e) =>
    {
        Debug.WriteLine($"Pronunciation assessment canceled. Reason: {e.Reason}");
        tcs.TrySetResult(null);
    };

    cancellationToken.Register(() =>
    {
        tcs.TrySetCanceled();
    });

    try
    {
        await recognizer.StartContinuousRecognitionAsync();
        return await tcs.Task.WaitAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
        Debug.WriteLine("Pronunciation assessment operation canceled.");
        return null;
    }
    finally
    {
        await recognizer.StopContinuousRecognitionAsync();
    }
}





    public async Task<PronunciationAssessmentResult> AssessPronunciation(
            CultureInfo culture,
            string referenceText,
            IProgress<string> recognitionResult,
            CancellationToken cancellationToken)
        {
            Debug.WriteLine($"Pronunciation Assessed here");
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechRecognitionLanguage = culture.Name;

            var pronunciationConfig = new PronunciationAssessmentConfig(
                referenceText,
                GradingSystem.HundredMark,
                Granularity.Phoneme);
            Debug.WriteLine($"pronunciationConfig: {pronunciationConfig}");

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            Debug.WriteLine($"audioConfig: {audioConfig}");
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            Debug.WriteLine($"recognizer: {recognizer}");

            pronunciationConfig.ApplyTo(recognizer);

            PronunciationAssessmentResult assessmentResult = null;

            recognizer.Recognizing += (s, e) =>
            {
                recognitionResult.Report(e.Result.Text);
            };

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    assessmentResult = PronunciationAssessmentResult.FromResult(e.Result);
                }
            };

            await recognizer.StartContinuousRecognitionAsync();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(200);
                }
            }
            finally
            {
                await recognizer.StopContinuousRecognitionAsync();
            }

            return assessmentResult;
        }
}
}