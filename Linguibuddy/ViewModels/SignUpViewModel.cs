using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Views;
using System.Text.RegularExpressions;

namespace Linguibuddy.ViewModels
{
    public partial class SignUpViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;

        [ObservableProperty]
        private string _email;
        [ObservableProperty]
        private string _username;
        [ObservableProperty]
        private string _password;
        [ObservableProperty]
        private float _labelUsernameErrorOpacity;
        [ObservableProperty]
        private float _labelEmailErrorOpacity;
        [ObservableProperty]
        private float _labelPasswordErrorOpacity;

        public SignUpViewModel(FirebaseAuthClient authClient, IServiceProvider services)
        {
            _authClient = authClient;
            _services = services;
            LabelUsernameErrorOpacity = 0;
            LabelEmailErrorOpacity = 0;
            LabelPasswordErrorOpacity = 0;
        }
        [RelayCommand]
        private async Task SignUp()
        {
            LabelUsernameErrorOpacity = 0;
            LabelEmailErrorOpacity = 0;
            LabelPasswordErrorOpacity = 0;

            if (string.IsNullOrWhiteSpace(Username))
                LabelUsernameErrorOpacity = 1;

            const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (string.IsNullOrWhiteSpace(Email) || !Regex.IsMatch(Email, pattern))
                LabelEmailErrorOpacity = 1;

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
                LabelPasswordErrorOpacity = 1;

            if (LabelUsernameErrorOpacity == 1 || LabelEmailErrorOpacity == 1 || LabelPasswordErrorOpacity == 1)
                return;

            await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);

            Application.Current.Windows[0].Page = new AppShell();
        }
        [RelayCommand]
        private async Task NavigateSignIn()
        {
            var signInPage = _services.GetRequiredService<SignInPage>();

            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
        }
    }
}
