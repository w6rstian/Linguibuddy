using Firebase.Auth;
using Linguibuddy.Views;

namespace Linguibuddy
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var authClient = IPlatformApplication.Current.Services.GetRequiredService<FirebaseAuthClient>();
            var user = authClient.User;
            if (user is null)
            {
                var signInPage = IPlatformApplication.Current.Services.GetService<SignInPage>();

                return new Window(new NavigationPage(signInPage));
            }
            else
            {
                return new Window(new AppShell());
            }
        }
    }
}