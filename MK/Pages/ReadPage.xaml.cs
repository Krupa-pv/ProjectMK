namespace MK;
using System.Diagnostics;
using MK.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.VisualBasic;

public partial class ReadPage : ContentPage
{

	private readonly ApiService _apiService;
	public ReadPage(ApiService apiService)
	{
		InitializeComponent();
        _apiService = apiService;
		
	}

	private async void OnGenerateStoryClicked(object sender, EventArgs e){

		var UserId = Preferences.Get("UserId", string.Empty);
		var difficulty = DifficultyPicker.SelectedItem?.ToString();
		var length = LengthPicker.SelectedItem?.ToString();

		if(string.IsNullOrEmpty(difficulty) ||string.IsNullOrEmpty(length) ){

			await DisplayAlert("Error", "Please select both difficulty and length.", "OK");
			return;
		}

		try{
			LoadingIndicator.IsRunning = true;
			LoadingIndicator.IsVisible = true;
			var response = await _apiService.GenerateStoryAsync(UserId, difficulty, length);
			string[] components = response.Split("Comprehension");
            Debug.WriteLine(components.Length);
            
            DisplayIMReader(components[0]);

		}
		catch(Exception ex){

			await DisplayAlert("Error", ex.Message, "OK");

		}
		
	}

	private async void DisplayIMReader(string story)
    {
        Debug.WriteLine("confirming entry complete");

        var response = await _apiService.getTokenAndSubdomainAsync();
        string token = response.Item1;
        string subdomain = response.Item2;
        string escapedStory = System.Web.HttpUtility.HtmlEncode(story);

		Debug.WriteLine(escapedStory);

        try{
            string immersiveReaderScript = $@"
                    <html>
                        <head>
                            <script src='https://ircdname.azureedge.net/immersivereadersdk/immersive-reader-sdk.1.4.0.js'></script>
                            <style>
                                body {{
                                    font-family: Arial, sans-serif;
                                    margin: 20px;
                                }}
                                h1 {{
                                    color: #2a5d84;
                                }}
                                p {{
                                    color: #333;
                                }}
                                .highlight {{
                                    background-color: yellow;
                                }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <button class='immersive-reader-button' data-button-style='iconAndText' data-locale='en'></button>

                                <h1 id='ir-title'>About Immersive Reader</h1>
                                <div id='ir-content' lang='en-us'>
                                    <p>
                                        {escapedStory}
                                    </p>
                                </div>
                            </div>

                            <script>
                                // Call the function to launch the Immersive Reader with the token and subdomain
                                function getTokenAndSubdomainAsync() {{
                                    return {{ token: '{token}', subdomain: '{subdomain}' }};
                                }}

                                function handleLaunchImmersiveReader() {{
                                    var response = getTokenAndSubdomainAsync();
                                    var token = response.token;
                                    var subdomain = response.subdomain;

                                    const data = {{
                                        title: document.getElementById('ir-title').innerText,
                                        chunks: [{{
                                            content: document.getElementById('ir-content').innerHTML,
                                            mimeType: 'text/html'
                                        }}]
                                    }};

                                    const options = {{
                                        onExit: function() {{ alert('Immersive Reader closed.'); }},
                                        uiZIndex: 2000
                                    }};

                                    ImmersiveReader.launchAsync(token, subdomain, data, options)
                                        .catch(function (error) {{
                                            alert('Error in launching Immersive Reader.');
                                            console.log(error);
                                        }});
                                }}

                                // Launch Immersive Reader after the page loads
                                handleLaunchImmersiveReader();

                            </script>
                        </body>
                    </html>";

                // Load the Immersive Reader content into the WebView
                ImmersiveReaderWebView.Source = new HtmlWebViewSource
                {
                    Html = immersiveReaderScript
                };
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
            }
            finally{
                LoadingIndicator.IsRunning = false;  // Stop loading spinner
                LoadingIndicator.IsVisible = false;
            }

    }



}