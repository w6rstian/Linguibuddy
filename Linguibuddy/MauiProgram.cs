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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PexelsDotNetSDK.Api;
using Plugin.Maui.Audio;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

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

            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream("Linguibuddy.appsettings.json");
            if (stream == null) throw new Exception("Nie znaleziono pliku appsettings.json w zasobach.");

            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("DEEPL_API_KEY", out JsonElement apiKeyElement))
            {
                throw new Exception("Nie znaleziono klucza DEEPL_API_KEY w JSON.");
            }

            var deeplApiKey = apiKeyElement.GetString();

            if (!root.TryGetProperty("GITHUB_TOKEN", out apiKeyElement))
            {
                throw new Exception("Nie znaleziono klucza GITHUB_TOKEN w JSON.");
            }

            var githubToken = apiKeyElement.GetString();

            if (!root.TryGetProperty("PEXELS_API_KEY", out apiKeyElement))
            {
                throw new Exception("Nie znaleziono klucza PEXELS_API_KEY w JSON.");
            }

            var pexelsApiKey = apiKeyElement.GetString();

            builder.Services.AddDbContext<DataContext>(
                options =>
                {
                    var dbPath = Path.Combine(FileSystem.AppDataDirectory, "database.db");
                    options.UseSqlite($"Data Source={dbPath}");
                }
                );

            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<MainViewModel>();

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

            builder.Services.AddTransient<AwardsPage>();
            builder.Services.AddTransient<AwardsViewModel>();

            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<SettingsViewModel>();

            builder.Services.AddTransient<AudioQuizPage>();
            builder.Services.AddTransient<AudioQuizViewModel>();

            builder.Services.AddTransient<MiniGamesPage>();
            builder.Services.AddTransient<MiniGamesViewModel>();

            builder.Services.AddTransient<ImageQuizPage>();
            builder.Services.AddTransient<ImageQuizViewModel>();

            builder.Services.AddTransient<SentenceQuizViewModel>();
            builder.Services.AddTransient<SentenceQuizPage>();

            builder.Services.AddTransient<HangmanViewModel>();
            builder.Services.AddTransient<HangmanPage>();

            //var deepLKey = Environment.GetEnvironmentVariable("DEEPL_API_KEY");
            //var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            //var githubAiKey = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var deepLKey = deeplApiKey;
            var githubAiKey = githubToken;

            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton(new DeepLTranslationService(deepLKey));
            builder.Services.AddSingleton(new OpenAiService(githubAiKey));
            builder.Services.AddTransient<MockDataSeeder>();
            builder.Services.AddTransient<FlashcardService>();

            builder.Services.AddHttpClient<DictionaryApiService>(client =>
            {
                client.BaseAddress = new Uri("https://api.dictionaryapi.dev/api/v2/entries/en/");
            });

            // 1. Rejestrujemy klienta z paczki NuGet (Singleton jest OK, bo to tylko wrapper API)
            builder.Services.AddSingleton<PexelsClient>(sp => new PexelsClient(pexelsApiKey));

            // 2. Rejestrujemy Twój serwis (jako zwykły serwis, już nie przez AddHttpClient)
            builder.Services.AddSingleton<PexelsImageService>();

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
