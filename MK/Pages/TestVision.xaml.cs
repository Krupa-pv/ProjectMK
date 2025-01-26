namespace MK;

using System.Diagnostics;
using LukeMauiFilePicker;
using MK.Services;
using MK.Drawables;
using SkiaSharp;





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

	private async void TapGestureRecognizer_Tapped(System.Object sender, Microsoft.Maui.Controls.TappedEventArgs e)
	{
		// Position relative to the container view, that is the image, the origin point is at the top left of the image.
		Point? relativeToContainerPosition = e.GetPosition((View)sender);
		double rawX = relativeToContainerPosition.Value.X;
		double rawY = relativeToContainerPosition.Value.Y;

		foreach (var box in boundingBoxes){

			if(rawY>box.Top && rawY<(box.Top+box.Height)){

				if(rawX>box.Left && rawX<box.Left+box.Width){
					Debug.WriteLine(box.Label);

				}

			}

		}
		
	}
	
}