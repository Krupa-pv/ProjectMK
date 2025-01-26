/*using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;

namespace MK.Services
{
    public class VisemeService
    {
        private readonly string _speechKey ;
        private readonly string _speechRegion ;

        public VisemeService()
        {
            // Retrieve key and region from environment variables
            

            if (string.IsNullOrEmpty(_speechKey) || string.IsNullOrEmpty(_speechRegion))
            {
                throw new InvalidOperationException("Speech key and region must be set as environment variables.");
            }
        }

        public async Task SpeakWithVisemesAsync(string text, Action<int, TimeSpan> handleViseme)
        {
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechSynthesisVoiceName = "en-US-AvaMultilingualNeural";

            using (var synthesizer = new SpeechSynthesizer(speechConfig, null))
            {
                //Suscribes to viseme received event
                synthesizer.VisemeReceived += (s, e) =>
                {
                    Console.WriteLine($"Viseme event received. Audio offset: {e.AudioOffset / 10000}ms, viseme id: {e.VisemeId}.");
                    
                    handleViseme?.Invoke(e.VisemeId, e.AudioOffset);
                };

                 // If VisemeID is the only thing you want, you can also use `SpeakTextAsync()`
                var result = await synthesizer.SpeakTextAsync(text);
                OutputSpeechSynthesisResult(result, text);
            }
        }

        private void OutputSpeechSynthesisResult(SpeechSynthesisResult result, string text)
        {
            switch (result.Reason)
            {
                case ResultReason.SynthesizingAudioCompleted:
                    Console.WriteLine($"Speech synthesized for text: [{text}]");
                    break;
                case ResultReason.Canceled:
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }
                    break;
                default:
                    break;
            }
        }
    }
}*/
