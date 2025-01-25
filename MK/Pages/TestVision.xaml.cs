namespace MK;
using LukeMauiFilePicker;
using MK.Services;



public partial class TestVision : ContentPage
{

	private readonly ApiService _apiService;

	readonly IFilePickerService picker;


	public TestVision(IFilePickerService picker)
	{
		this.picker=picker;
		InitializeComponent();
		_apiService = new ApiService();

	}

	private async void OnFileUpload(object sender, EventArgs e){
            Image post = await _apiService.uploadFileToBackend(picker);

    }
	
}