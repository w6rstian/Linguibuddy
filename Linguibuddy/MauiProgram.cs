using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using LocalizationResourceManager.Maui;
using Microsoft.Extensions.Logging;

namespace Linguibuddy
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalizationResourceManager(settings =>
                {
                    settings.RestoreLatestCulture(true);
                    settings.AddResource(AppResources.ResourceManager);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            var deepLKey = Environment.GetEnvironmentVariable("DEEPL_API_KEY");

            if (string.IsNullOrEmpty(deepLKey))
            {
                System.Diagnostics.Debug.WriteLine("⚠DEEPL_API_KEY is not set in environment variables.");
            }

            builder.Services.AddSingleton(new DeepLTranslationService(deepLKey));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
