using Microsoft.Extensions.Logging;
using MK.Services;
using Plugin.Maui.Audio;


#if MACCATALYST
using MK.Platforms.MacCatalyst;
#elif IOS
using MK.Platforms.iOS;
#endif

namespace MK;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("montserrat-latin-600-normal.ttf", "Mont");
				fonts.AddFont("Montserrat-Italic-VariableFont_wght.ttf", "Mont-I");
				
			});

		//registering the audio service from plugin.maui.audio
		builder.Services.AddSingleton<IAudioManager, AudioManager>();
		builder.Services.AddSingleton<ApiService>();

		#if MACCATALYST
        		builder.Services.AddSingleton<ISpeechToText, SpeechToTextImplementation>();
		#elif IOS
				builder.Services.AddSingleton<ISpeechToText, SpeechToTextImplementation>();
		#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
