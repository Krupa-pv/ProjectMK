namespace MK;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

public partial class AppShell : Shell
{
	public AppShell()
    {
        InitializeComponent();
        CheckUserIdAndNavigate();
    }

    private async void CheckUserIdAndNavigate()
    {
        // Check if UserId is already saved
        if (!Preferences.ContainsKey("UserId"))
        {
            // First-time user setup as no user id is present
            await GoToAsync("//UserInput");  // Navigate to UserInput page
        }
        else
        {
            // User already exists, so loads the reading page
            await GoToAsync("//MainPage");  // Navigate to ReadPage
        }
    }
}