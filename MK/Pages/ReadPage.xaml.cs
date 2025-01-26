namespace MK;
using System.Diagnostics;
using MK.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.VisualBasic;

public partial class ReadPage : ContentPage
{

        int[] correct = new int[5];


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
            parseQuestions(components[1]);

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

     private async void parseQuestions (string OAInput){

        StackLayout[] allQuestions = {Question1, Question2, Question3, Question4, Question5};
        Label[] allLabels = {Label1, Label2, Label3, Label4, Label5};
        RadioButton[] allAnswers = {Answer1, Answer2, Answer3, Answer4, Answer5, Answer6, Answer7, Answer8, Answer9, Answer10, Answer11, Answer12, Answer13, Answer14, Answer15, Answer16, Answer17, Answer18, Answer19, Answer20};

        Debug.WriteLine(OAInput);
        List<String> parsedElements = new List<String>();

        int endIndex = -1;
        for(int i = 1; i <= 5; i++){
            string num = i.ToString();
            int index = OAInput.IndexOf(num+".");

            string[] searchKeys = {"a)","b)","c)","d)"};
            endIndex = OAInput.Substring(index+3).IndexOf(searchKeys[0]);


            string question = OAInput.Substring(index+3,endIndex);
            Debug.WriteLine(question);
            parsedElements.Add(question);

            int correctA = -1;

            int firstIndex;
            int secondIndex;
            for(int j = 0; j < 3; j++){

                firstIndex = OAInput.Substring(index+3).IndexOf(searchKeys[j]);
                secondIndex = OAInput.Substring(index+3).IndexOf(searchKeys[j+1]);
                string answerChoice = OAInput.Substring(index+3).Substring(firstIndex+3, secondIndex-firstIndex-6);
                if(answerChoice.IndexOf("(C)")!=-1){
                    correctA = j;
                    answerChoice = answerChoice.Substring(0, answerChoice.IndexOf("(C)")-1);
                }
                Debug.WriteLine(answerChoice);
                parsedElements.Add(answerChoice);
            }
            string lastChoice;
            firstIndex = OAInput.Substring(index+3).IndexOf(searchKeys[3]);
            string num2 = (i+1).ToString();
            secondIndex = OAInput.Substring(index+3).IndexOf(num2+".");
            if(secondIndex==-1){
                lastChoice = OAInput.Substring(index+3).Substring(firstIndex+3);
            }
            else{
                lastChoice = OAInput.Substring(index+3).Substring(firstIndex+3, secondIndex-firstIndex-6);
            }
            if(lastChoice.IndexOf("(C)")!=-1){
                    correctA = 3;
                    lastChoice = lastChoice.Substring(0, lastChoice.IndexOf("(C)")-1);

                }
            Debug.WriteLine(lastChoice);
            parsedElements.Add(lastChoice);
            Debug.WriteLine(correctA);
            parsedElements.Add(correctA.ToString());
        }
        int currentAnswer = 0;
        for(int i = 0; i < 5; i++){
            allQuestions[i].IsVisible = !allQuestions[i].IsVisible;
            allLabels[i].Text = parsedElements[6*i];
            for(int j = 0; j < 4; j++){
                allAnswers[currentAnswer].Content = parsedElements[6*i+1+j];
                currentAnswer++;
            }
            correct[i]=Int32.Parse(parsedElements[6*i+5]);
        }

    }


    private async void OnSubmit(object sender, EventArgs e){



    }



}