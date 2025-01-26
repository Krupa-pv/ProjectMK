namespace MK;
using MK.Services;

public partial class PracticePage : ContentPage
{

	private readonly ApiService _apiService;
    private readonly ISpeechToText _speechToText;

	public PracticePage(ApiService apiService, ISpeechToText speechToText)
	{
		InitializeComponent();
		_apiService = apiService;
        _speechToText = speechToText;

	}

	private async void OnReading(object sender, EventArgs e)
        {
            // Navigate to WordSpeakPage
            //await Navigation.PushAsync(new TestVision(_apiService, picker));
        }

        private async void OnPronunciation(object sender, EventArgs e)
        {
            // Navigate to SpeechPage
            await Navigation.PushAsync(new SpeechPage(_apiService, _speechToText));
        }
}