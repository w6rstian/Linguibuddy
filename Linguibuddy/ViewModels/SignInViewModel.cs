using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public partial class SignInViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;

        [ObservableProperty]
        private string _email;
        [ObservableProperty]
        private string _password;

        public SignInViewModel(FirebaseAuthClient authClient, IServiceProvider services)
        {
            _authClient = authClient;
            _services = services;
        }
        [RelayCommand]
        private async Task SignIn()
        {
            try
            {
                await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);
            }
            catch (Exception ex)
            {
                // TODO: show error message
                return;
            }

            //await Shell.Current.GoToAsync("//MainPage");
            Application.Current.Windows[0].Page = new AppShell();
        }
        [RelayCommand]
        private async Task NavigateSignUp()
        {
            var signUpPage = _services.GetRequiredService<SignUpPage>();

            Application.Current.Windows[0].Page = new NavigationPage(signUpPage);
        }
    }
}
