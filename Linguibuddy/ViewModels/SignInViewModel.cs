using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
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

        [ObservableProperty]
        private string _email;
        [ObservableProperty]
        private string _password;

        public SignInViewModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
        }
        [RelayCommand]
        private async Task SignIn()
        {
            await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);

            if (_authClient.User is not null)
            {
                await Shell.Current.GoToAsync("//MainPage");
            }

            // TODO: error handling and navigating to home
        }
        [RelayCommand]
        private async Task NavigateSignUp()
        {
            await Shell.Current.GoToAsync("//SignUp");
        }
    }
}
