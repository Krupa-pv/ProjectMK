using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace MK.Services;

public class TextToSpeechService 
{
    private readonly ApiService _apiService;
    private string _speechKey;
    private string _speechRegion;

    public TextToSpeechService(ApiService apiService)
    {
         _apiService = apiService;
    }

    /*private async Task InitializeSpeechConfigAsync()
    {
        if (string.IsNullOrEmpty(_speechKey) || string.IsNullOrEmpty(_speechRegion))
        {
            // Retrieve key and region from ApiService
            var speechConfigData = await _apiService.GetSpeechInfo();
            _speechKey = speechConfigData.Key;
            _speechRegion = speechConfigData.Region;

        }
    }*/

    public async Task SpeakTextAsync(string text)
        {
            var response = await _apiService.GetSpeechInfo();
            string _speechKey = response.Item1;
            string _speechRegion = response.Item2;
            var speechConfig = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
            speechConfig.SpeechSynthesisVoiceName = "en-US-SerenaMultilingualNeural";

            using (var speechSynthesizer = new SpeechSynthesizer(speechConfig))
            {
                var result = await speechSynthesizer.SpeakTextAsync(text);
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
    