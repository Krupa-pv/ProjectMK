using Microsoft.Extensions.Logging;
using MK.Services;
using Plugin.Maui.Audio;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using TextEncoding = System.Text.Encoding;

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

        // Registering the audio service from Plugin.Maui.Audio
        builder.Services.AddSingleton<IAudioManager, AudioManager>();
        builder.Services.AddSingleton<ApiService>();

        /*var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MK.appsettings.json");
        if (stream == null) throw new Exception("Embedded resource not found!");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        var configBuilder = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(TextEncoding.UTF8.GetBytes(json)));

        var configuration = configBuilder.Build();
        */
        // Platform-specific services
        #if MACCATALYST
        builder.Services.AddSingleton<ISpeechToText, SpeechToTextImplementation>();
        #elif IOS
        builder.Services.AddSingleton<ISpeechToText, SpeechToTextImplementation>();
        #endif

        #if DEBUG
        builder.Logging.AddDebug();
        #endif
        builder.Services.AddFilePicker();

        return builder.Build();
    }
}
