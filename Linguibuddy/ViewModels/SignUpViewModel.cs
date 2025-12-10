using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Views;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public SignUpViewModel(FirebaseAuthClient authClient, IServiceProvider services)
        {
            _authClient = authClient;
            _services = services;
        }
        [RelayCommand]
        private async Task SignUp()
        {
            if (Email == "" || Password.Length < 6 || Username == "")
            {
                // TODO Error handling, message for the user
                return;
            }
            await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);
            var signInPage = _services.GetRequiredService<SignInPage>();

            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
        }
        [RelayCommand]
        private async Task NavigateSignIn()
        {
            var signInPage = _services.GetRequiredService<SignInPage>();

            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
        }
    }
}
