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

public partial class ISpeak : ContentPage
{
    private readonly ApiService _apiService;
    private readonly ISpeechToText _speechToText;
    private readonly TextToSpeechService _TTS;
    
    public ISpeak(ApiService apiService, ISpeechToText speechToText, TextToSpeechService tts)
    {
        Debug.WriteLine("Navigating to speech page");
        InitializeComponent();
        _apiService = apiService;
        _speechToText = speechToText;
        _TTS = tts;
    }

    private async void OnWordSpeakClicked(object sender, EventArgs e)
        {
            // Navigate to WordSpeakPage
            Debug.WriteLine("Navigating to word page");
            await Navigation.PushAsync(new WordSpeakPage(_apiService, _speechToText, _TTS));
        }

        private async void OnSpeechPageClicked(object sender, EventArgs e)
        {
            // Navigate to SpeechPage
            Debug.WriteLine("Navigating to speechstory page");
            await Navigation.PushAsync(new SpeechPage(_apiService, _speechToText));
        }
}

