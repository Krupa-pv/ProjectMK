using AuthenticationServices;

using MK.Services;
using MK.Models;


namespace MK;

public partial class UserInput : ContentPage
{
    private readonly ApiService _apiService;

    private string _name;
    private string _age;
    private string _grade;
    private string _readinglevel;
    private List<string> _interests = new List<string> ();

	public UserInput()
	{
		InitializeComponent();
        _apiService = new ApiService();
	}

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        string oldText = e.OldTextValue;
        string newText = e.NewTextValue;
        string myText = entry.Text;
		//Debug.WriteLine("confirmation using this");
    } 

    private void OnEntryCompleted(object sender, EventArgs e)
    {
        _name = ((Entry)sender).Text;
		//Debug.WriteLine("confirming entry complete");

    }

    private void OnAgeSelected(object sender, EventArgs e){
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex != -1)
        {
            _age = (string)picker.ItemsSource[selectedIndex];
        }
        

    }

    private void OnSelectedIndexChanged(object sender, EventArgs e){

        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex != -1)
        {
            _grade = (string)picker.ItemsSource[selectedIndex];
            //Debug.WriteLine("okur chose the grade: " + _grade);

        }


    }

    private void OnReadingLevelChanged(object sender, EventArgs e){
        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        if (selectedIndex != -1)
        {
            _readinglevel = (string)picker.ItemsSource[selectedIndex];
            //Debug.WriteLine("okur chose the grade: " + _grade);

        }
    }

    private void OnInterestsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _interests = e.CurrentSelection.Cast<string>().ToList(); // Get selected items as list of strings
    }


    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = _name,
            Age = _age,
            Grade = _grade,
            ReadingLevel = _readinglevel,
            Interests = string.Join(", ", _interests),
            TroubleWords = new List<TroubleWord>(),
            SpeechFeedback = new List<PronunciationFeedback>(),
            ReadingScores = new List<int>(),
            LastSpeechPracticeDate = new DateTime()
        
        };
        
        //This will save the userid and username locally
        Preferences.Set("UserId", user.Id);
        Preferences.Set("UserName", user.Name);

        var success = await _apiService.AddUserAsync(user);

        await DisplayAlert("Result", success ? "User saved successfully!" : "Failed to save user.", "OK");
    }

}