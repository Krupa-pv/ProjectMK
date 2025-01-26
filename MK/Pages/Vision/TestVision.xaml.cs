namespace MK;

using System.Diagnostics;
using LukeMauiFilePicker;
using MK.Services;
using SkiaSharp;





public partial class TestVision : ContentPage
{
	private List<BoundingBoxResult> boundingBoxes;

	private readonly ApiService _apiService;

	readonly IFilePickerService picker;


	public TestVision(ApiService apiService, IFilePickerService picker)
	{
		this.picker=picker;
		InitializeComponent();
		_apiService = apiService;

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
		TextToSpeechService tS = new TextToSpeechService(_apiService);
		float originalWidth = 0;
		float originalHeight = 0;
		double scale = 0;
		if(boundingBoxes[0]!=null){
			 originalWidth = boundingBoxes[0].ImageWidth;
			 originalHeight = boundingBoxes[0].ImageHeight;
			 scale = ((showSelect.Height)/(originalHeight));
			 Debug.WriteLine("scale: "+ scale);
		}
		Debug.WriteLine("x: "+ rawX +"y: "+rawY);

		string hi = null;

		double pastDist = 1000000000;
		//bowl: 719, 317
		//person: 651, 195.626

		foreach (var box in boundingBoxes){

			double topC = box.Top*scale;
			double leftC = (Container.Width-(scale*originalWidth))/2 + box.Left*scale;
			double heightC = box.Height*scale;
			double widthC = box.Width*scale;


			if(rawY>topC && rawY<(topC+heightC)){

				if(rawX>leftC && rawX<(leftC+widthC)){


					if(hi!=null){
						double newDist = await calculateDistance(rawX,rawY, leftC, topC, leftC+widthC, topC+heightC);
						if(newDist<pastDist){

							pastDist = await calculateDistance(rawX,rawY, leftC, topC, leftC+widthC, topC+heightC);
							hi = box.Label;

						}
					}
					else{
						pastDist = await calculateDistance(rawX,rawY, leftC, topC, leftC+widthC, topC+heightC);
						hi = box.Label;
					}

				}

			}

		}

		if(hi!=null){
			ClickedWord.Text = hi;
			tS.SpeakTextAsync(hi);
		}
		
		
	}

	private async Task<double> calculateDistance(double x1,double y1, double xb1,double yb1,double xb2,double yb2){
		double midBoxX = (xb2-xb1)/2+xb1;
		double midBoxY = (yb2-yb1)/2+yb1;

		return Math.Sqrt(((x1-midBoxX)*(x1-midBoxX))+((y1-midBoxY)*(y1-midBoxY)));


	}
	
}