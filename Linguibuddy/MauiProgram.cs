using CommunityToolkit.Maui;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Linguibuddy.ViewModels;
using Linguibuddy.Views;
using LocalizationResourceManager.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using Linguibuddy.Views;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;

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

            builder.Services.AddSingleton(new DeepLTranslationService(deepLKey));
            builder.Services.AddSingleton(new OpenAiService(githubAiKey));
            builder.Services.AddSingleton<DictionaryApiService>();

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

            builder.Services.AddTransient<SignInPage>();
            builder.Services.AddTransient<SignInViewModel>();
            builder.Services.AddTransient<SignUpPage>();
            builder.Services.AddTransient<SignUpViewModel>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            /*
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                context.Database.EnsureCreated();

                if (!context.Users.Any())
                {
                    context.Users.AddRange(
                        new Models.User { UserName = "admin" },
                        new Models.User { UserName = "testuser" }
                        );
                    context.SaveChanges();
                }
            }
            */
            return app;
        }
    }
}
