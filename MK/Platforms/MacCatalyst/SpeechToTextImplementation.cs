using System.Globalization;
using System.Threading;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using MK;

namespace MK.Platforms.MacCatalyst
{
    public class SpeechToTextImplementation : ISpeechToText
    {
        private string _speechKey = "6p0SyYUApqwf9BSJ6MrqvuGP08yWOuig18Kc8cfX0btiAnd7dQm8JQQJ99BAACYeBjFXJ3w3AAAYACOGYUIR"; // Replace with your Azure Speech Key
        private string _speechRegion = "eastus";       // Replace with your Azure Speech Region

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

        public async Task<string> Listen(CultureInfo culture, IProgress<string> recognitionResult, CancellationToken cancellationToken)
        {
            if (!await RequestPermissions())
            {
                throw new InvalidOperationException("Microphone permissions are required.");
            }

            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechRecognitionLanguage = culture.Name;

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            string recognizedText = string.Empty;

            recognizer.Recognizing += (s, e) =>
            {
                recognitionResult.Report(e.Result.Text);
            };

            recognizer.Recognized += (s, e) =>
            {
                recognizedText = e.Result.Text;
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

            return recognizedText;
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

        public async Task<PronunciationAssessmentResult> DiscreteAssessPronunciation(
    CultureInfo culture,
    string referenceText,
    IProgress<string> recognitionResult,
    CancellationToken cancellationToken)
{
    Debug.WriteLine("Starting Pronunciation Assessment...");

    // Configure speech settings
    var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
    speechConfig.SpeechRecognitionLanguage = culture.Name;

    // Configure pronunciation assessment
    var pronunciationConfig = new PronunciationAssessmentConfig(
        referenceText,
        GradingSystem.HundredMark,
        Granularity.Phoneme,
        enableMiscue: true);

    using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
    using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

    Debug.WriteLine("Applying Pronunciation Config...");
    pronunciationConfig.ApplyTo(recognizer);

    // Use TaskCompletionSource to handle async event result
    var taskCompletionSource = new TaskCompletionSource<PronunciationAssessmentResult>();

    recognizer.Recognized += (s, e) =>
{
    if (e.Result.Reason == ResultReason.RecognizedSpeech)
    {
        Debug.WriteLine($"Recognized Speech: {e.Result.Text}");
        recognitionResult?.Report(e.Result.Text);

        var assessmentResult = PronunciationAssessmentResult.FromResult(e.Result);
        Debug.WriteLine($"Assessment Result: Accuracy={assessmentResult.AccuracyScore}, Fluency={assessmentResult.FluencyScore}");

        taskCompletionSource.TrySetResult(assessmentResult);
    }
    else if (e.Result.Reason == ResultReason.NoMatch)
    {
        Debug.WriteLine("No match found for the spoken text.");
        Debug.WriteLine($"Raw Recognition Text: {e.Result.Text ?? "Empty"}"); // Log empty or unmatched speech
        taskCompletionSource.TrySetResult(null);
    }
};

    recognizer.Canceled += (s, e) =>
    {
        Debug.WriteLine($"Recognition canceled. Reason: {e.Reason}");
        if (e.Reason == CancellationReason.Error)
        {
            Debug.WriteLine($"Error details: {e.ErrorDetails}");
        }
        taskCompletionSource.TrySetResult(null);
    };

    Debug.WriteLine("Starting Speech Recognition...");
    await recognizer.StartContinuousRecognitionAsync();

    try
    {
        using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()))
        {
            // Wait for a result or cancellation
            return await taskCompletionSource.Task;
        }
    }
    catch (TaskCanceledException)
    {
        Debug.WriteLine("Speech recognition canceled.");
        return null;
    }
    finally
    {
        Debug.WriteLine("Stopping Speech Recognition...");
        await recognizer.StopContinuousRecognitionAsync();
    }
}


    }
}