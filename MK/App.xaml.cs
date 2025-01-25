namespace MK;
using Microsoft.Maui;
using Microsoft.Maui.Controls;


public partial class App : Application
{
	public App()
	{

		InitializeComponent();
		//MainPage = new NavigationPage(new MainPage());

		//MainPage = new AppShell(); //this will ensure that appshell is the starting pt of app
	}
	

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
	
}