namespace MK;
using MK.Services;
using LukeMauiFilePicker;



public partial class LearnPage : ContentPage
{

    private readonly ApiService _apiService;
    private readonly ISpeechToText _speechToText;
    private readonly TextToSpeechService _TTS;

	readonly IFilePickerService picker;

	public LearnPage(ApiService apiService, ISpeechToText speechToText, IFilePickerService picker, TextToSpeechService tts)
	{
		InitializeComponent();
		_apiService = apiService;
        _speechToText = speechToText;
        _TTS = tts;
		this.picker=picker;

	}

	private async void OnVocab(object sender, EventArgs e)
        {
            // Navigate to WordSpeakPage
            await Navigation.PushAsync(new TestVision(_apiService, picker));
        }

        private async void OnPronunciation(object sender, EventArgs e)
        {
            // Navigate to SpeechPage
            await Navigation.PushAsync(new WordSpeakPage(_apiService, _speechToText, _TTS));
        }

}