﻿namespace MK;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

public partial class MainPage : ContentPage
{
	int count = 0;


	public MainPage()
	{
		InitializeComponent();
	}
	private async void OnCounterClicked(object sender, EventArgs e)
	{

		await Navigation.PushAsync(new UserInput());

	}
	

}

