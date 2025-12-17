using CommunityToolkit.Maui;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using Linguibuddy.Data;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Linguibuddy.ViewModels;
using Linguibuddy.Views;
using LocalizationResourceManager.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;

namespace Linguibuddy
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
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

            builder.Services.AddDbContext<DataContext>(
                options =>
                {
                    var dbPath = Path.Combine(FileSystem.AppDataDirectory, "database.db");
                    options.UseSqlite($"Data Source={dbPath}");
                }
                );

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainViewModel>();

            builder.Services.AddTransient<DictionaryPage>();
            builder.Services.AddTransient<DictionaryViewModel>();

            builder.Services.AddTransient<FlashcardsPage>();
            builder.Services.AddTransient<FlashcardsViewModel>();

            builder.Services.AddTransient<FlashcardsCollectionsPage>();
            builder.Services.AddTransient<FlashcardsCollectionsViewModel>();

            builder.Services.AddTransient<SignInPage>();
            builder.Services.AddTransient<SignInViewModel>();

            builder.Services.AddTransient<SignUpPage>();
            builder.Services.AddTransient<SignUpViewModel>();

            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsViewModel>();


            var deepLKey = Environment.GetEnvironmentVariable("DEEPL_API_KEY");
            //var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var githubAiKey = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

            if (string.IsNullOrEmpty(deepLKey))
            {
                System.Diagnostics.Debug.WriteLine("DEEPL_API_KEY is not set in environment variables.");
            }

            if (string.IsNullOrEmpty(githubAiKey))
            {
                System.Diagnostics.Debug.WriteLine("GITHUB_TOKEN is not set in environment variables.");
            }

            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton(new DeepLTranslationService(deepLKey));
            builder.Services.AddSingleton(new OpenAiService(githubAiKey));
            builder.Services.AddTransient<DictionaryApiService>();
            builder.Services.AddTransient<FlashcardService>();

            builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig()
            {
                ApiKey = "AIzaSyDzvckq_urVWkkgHCTbgeDK3MTHq6GzFmk",
                AuthDomain = "linguibuddy.web.app",
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                },
                UserRepository = new FileUserRepository("Linguibuddy")
            }));

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                // ODKOMENTUJ TĘ LINIJKĘ, ABY ZRESETOWAĆ BAZĘ:
                //context.Database.EnsureDeleted();

                context.Database.EnsureCreated();
            }

            return app;
        }
    }
}
