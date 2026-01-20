using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Views;

namespace Linguibuddy;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (IPlatformApplication.Current != null)
        {
            var authClient = IPlatformApplication.Current.Services.GetRequiredService<FirebaseAuthClient>();
            var user = authClient.User;
            if (
                user is null ||
                !IPlatformApplication.Current.Services.GetRequiredService<DataContext>()
                    .AppUsers
                    .Any(au => au.Id == user.Uid))
            {
                var signInPage = IPlatformApplication.Current.Services.GetService<SignInPage>();

                return new Window(new NavigationPage(signInPage));
            }
        }

        return new Window(GetMainShell());
    }

    public static Page GetMainShell()
    {
        if (DeviceInfo.Idiom == DeviceIdiom.Phone) return new MobileShell();

        return new AppShell();
    }

    public static void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(FlashcardsPage), typeof(FlashcardsPage));
        Routing.RegisterRoute(nameof(CollectionDetailsPage), typeof(CollectionDetailsPage));
        Routing.RegisterRoute(nameof(AudioQuizPage), typeof(AudioQuizPage));
        Routing.RegisterRoute(nameof(ImageQuizPage), typeof(ImageQuizPage));
        Routing.RegisterRoute(nameof(SentenceQuizPage), typeof(SentenceQuizPage));
        Routing.RegisterRoute(nameof(HangmanPage), typeof(HangmanPage));
        Routing.RegisterRoute(nameof(SpeakingQuizPage), typeof(SpeakingQuizPage));
    }
}