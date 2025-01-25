using System.Globalization;
using System.Threading;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Diagnostics;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using MK;

namespace MK.Platforms.iOS{

    public class SpeechToTextImplementation : ISpeechToText
    {
        private readonly string _speechKey = "6p0SyYUApqwf9BSJ6MrqvuGP08yWOuig18Kc8cfX0btiAnd7dQm8JQQJ99BAACYeBjFXJ3w3AAAYACOGYUIR"; // Replace with your Azure Speech Key
        private readonly string _speechRegion = "eastus";       // Replace with your Azure Speech Region

        public async Task<bool> RequestPermissions()
        {
            // macOS/iOS microphone permissions are managed via Info.plist
            // Return true assuming permissions are properly configured
            return await Task.FromResult(true);
        }

        public async Task<string> Listen(CultureInfo culture, IProgress<string> recognitionResult, CancellationToken cancellationToken)
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechRecognitionLanguage = culture.Name;

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            string recognizedText = string.Empty;

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
                    recognizedText = e.Result.Text;
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

            return recognizedText;
        }

        public async Task<PronunciationAssessmentResult> AssessPronunciation(
            CultureInfo culture,
            string referenceText,
            IProgress<string> recognitionResult,
            CancellationToken cancellationToken)
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechRecognitionLanguage = culture.Name;

            // Configure pronunciation assessment
            var pronunciationConfig = new PronunciationAssessmentConfig(
                referenceText,
                GradingSystem.HundredMark,
                Granularity.Phoneme);

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            pronunciationConfig.ApplyTo(recognizer);

            PronunciationAssessmentResult assessmentResult = null;

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
                Granularity.Phoneme);

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            Debug.WriteLine("Applying Pronunciation Config...");
            pronunciationConfig.ApplyTo(recognizer);

            PronunciationAssessmentResult assessmentResult = null;

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    Debug.WriteLine($"Recognized Speech: {e.Result.Text}");
                    recognitionResult?.Report(e.Result.Text);

                    assessmentResult = PronunciationAssessmentResult.FromResult(e.Result);
                    Debug.WriteLine($"Assessment Result: Accuracy={assessmentResult.AccuracyScore}, Fluency={assessmentResult.FluencyScore}");

                    // Log word-level results if available
                    foreach (var word in assessmentResult.Words)
                    {
                        Debug.WriteLine($"Word: {word.Word}, Accuracy: {word.AccuracyScore}");
                    }
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Debug.WriteLine("No speech could be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                Debug.WriteLine($"Recognition canceled. Reason: {e.Reason}");
                if (e.Reason == CancellationReason.Error)
                {
                    Debug.WriteLine($"Error details: {e.ErrorDetails}");
                }
            };

            Debug.WriteLine("Starting Speech Recognition...");
            await recognizer.StartContinuousRecognitionAsync();

            try
            {
                // Wait for a single utterance or cancellation
                while (!cancellationToken.IsCancellationRequested && assessmentResult == null)
                {
                    await Task.Delay(200);
                }
            }
            finally
            {
                Debug.WriteLine("Stopping Speech Recognition...");
                await recognizer.StopContinuousRecognitionAsync();
            }

            if (assessmentResult == null)
            {
                Debug.WriteLine("Assessment Result is null. No speech was recognized.");
            }

            return assessmentResult;
        }


    }
}

