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
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;

        [ObservableProperty]
        private string _displayName;
        [ObservableProperty]
        private string _email;
        [ObservableProperty]
        private string _photoUrl;
        public ProfileViewModel(FirebaseAuthClient authClient, IServiceProvider services)
        {
            _authClient = authClient;
            _services = services;
            LoadUserData();
        }

        private void LoadUserData()
        {
            var user = _authClient.User.Info;

            DisplayName = user.DisplayName;
            Email = user.Email;
            if (string.IsNullOrEmpty(user.PhotoUrl))
            {
                PhotoUrl = Application.Current.RequestedTheme == AppTheme.Light
                           ? "person_120dp_light.png"
                           : "person_120dp_dark.png";
            }
            else
            {
                PhotoUrl = user.PhotoUrl;
            }
        }

        [RelayCommand]
        private async Task SignOut()
        {
            _authClient.SignOut();
            var signInPage = _services.GetRequiredService<SignInPage>();
            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
        }
    }
}
