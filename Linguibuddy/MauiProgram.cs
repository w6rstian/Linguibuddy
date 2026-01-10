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
using Newtonsoft.Json.Linq;
using PexelsDotNetSDK.Api;
using Plugin.Maui.Audio;
using System.Reflection;

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

            var settings = JObject.Parse(json);

            var deeplApiKey = settings["DEEPL_API_KEY"]?.ToString();
            if (string.IsNullOrEmpty(deeplApiKey))
                throw new Exception("Nie znaleziono klucza DEEPL_API_KEY w JSON.");

            var githubToken = settings["GITHUB_TOKEN"]?.ToString();
            if (string.IsNullOrEmpty(githubToken))
                throw new Exception("Nie znaleziono klucza GITHUB_TOKEN w JSON.");

            var pexelsApiKey = settings["PEXELS_API_KEY"]?.ToString();
            if (string.IsNullOrEmpty(pexelsApiKey))
                throw new Exception("Nie znaleziono klucza PEXELS_API_KEY w JSON.");

            builder.Services.AddDbContext<DataContext>(
                options =>
                {
                    var dbPath = Path.Combine(FileSystem.AppDataDirectory, "database.db");
                    options.UseSqlite($"Data Source={dbPath}");
                }
                );

            builder.Services.AddTransient<MainPage, MainViewModel>();
            builder.Services.AddTransient<DictionaryPage, DictionaryViewModel>();
            builder.Services.AddTransient<FlashcardsPage, FlashcardsViewModel>();
            builder.Services.AddTransient<FlashcardsCollectionsPage, FlashcardsCollectionsViewModel>();
            builder.Services.AddTransient<SignInPage, SignInViewModel>();
            builder.Services.AddTransient<SignUpPage, SignUpViewModel>();
            builder.Services.AddTransient<AchievementsPage, AchievementsViewModel>();
            builder.Services.AddTransient<SettingsPage, SettingsViewModel>();
            builder.Services.AddTransient<AudioQuizPage, AudioQuizViewModel>();
            builder.Services.AddTransient<MiniGamesPage, MiniGamesViewModel>();
            builder.Services.AddTransient<ImageQuizPage, ImageQuizViewModel>();
            builder.Services.AddTransient<SentenceQuizPage, SentenceQuizViewModel>();
            builder.Services.AddTransient<HangmanPage, HangmanViewModel>();            
            builder.Services.AddTransientPopup<WordCollectionPopup, WordCollectionPopupViewModel>();

            builder.Services.AddTransient<AchievementService>();

            //var deepLKey = Environment.GetEnvironmentVariable("DEEPL_API_KEY");
            //var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            //var githubAiKey = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var deepLKey = deeplApiKey;
            var githubAiKey = githubToken;

            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddSingleton(new DeepLTranslationService(deepLKey));
            builder.Services.AddSingleton(new OpenAiService(githubAiKey));
            builder.Services.AddTransient<MockDataSeeder>();
            builder.Services.AddTransient<CollectionService>();

            builder.Services.AddHttpClient<DictionaryApiService>(client =>
            {
                client.BaseAddress = new Uri("https://api.dictionaryapi.dev/api/v2/entries/en/");
            });

            builder.Services.AddSingleton<PexelsClient>(sp => new PexelsClient(pexelsApiKey));
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
                context.Database.EnsureDeleted();

                context.Database.EnsureCreated();
            }

            return app;
        }
    }
}
