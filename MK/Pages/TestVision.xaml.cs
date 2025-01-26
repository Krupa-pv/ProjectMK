namespace MK;

using System.Diagnostics;
using LukeMauiFilePicker;
using MK.Services;
using MK.Drawables;




public partial class TestVision : ContentPage
{
	private List<BoundingBoxResult> boundingBoxes;

	private readonly ApiService _apiService;

	readonly IFilePickerService picker;


	public TestVision(IFilePickerService picker)
	{
		this.picker=picker;
		InitializeComponent();
		_apiService = new ApiService();

	}

	private async void OnFileUpload(object sender, EventArgs e){
            ImageSource imageSource = await _apiService.uploadFileToBackend(picker);
			if (imageSource != null)
			{
				showSelect.Source = imageSource; // Set the ImageSource for the image control
				boundingBoxes = await _apiService.getBoxes();

				Debug.WriteLine($"Bounding boxes count: {boundingBoxes.Count}");
				foreach (var box in boundingBoxes)
				{
					Debug.WriteLine($"Label: {box.Label}, Left: {box.Left}, Top: {box.Top}, Width: {box.Width}, Height: {box.Height}");
				}
			}
			else
			{
				Debug.WriteLine("Failed to display the image.");
			}
    }
	
}