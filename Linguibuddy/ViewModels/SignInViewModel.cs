using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Views;

namespace Linguibuddy.ViewModels
{
    public partial class SignInViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;
        private readonly DataContext db;

        [ObservableProperty]
        private string _email;
        [ObservableProperty]
        private string _password;
        [ObservableProperty]
        private float _labelErrorOpacity;

        public SignInViewModel(FirebaseAuthClient authClient, IServiceProvider services, DataContext dataContext)
        {
            _authClient = authClient;
            _services = services;
            db = dataContext;
            LabelErrorOpacity = 0;
        }
        [RelayCommand]
        private async Task SignIn()
        {
            LabelErrorOpacity = 0;
            try
            {
                await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);
            }
            catch (Exception ex)
            {
                LabelErrorOpacity = 1;
                return;
            }

            var appUser = await db.AppUsers.FindAsync(_authClient.User.Uid);
            if (appUser == null)
            {
                appUser = new AppUser
                {
                    Id = _authClient.User.Uid
                };
                db.AppUsers.Add(appUser);
                await db.SaveChangesAsync();
            }
            //await Shell.Current.GoToAsync("//MainPage");
            Application.Current!.Windows[0].Page = App.GetMainShell();
        }
        [RelayCommand]
        private async Task NavigateSignUp()
        {
            var signUpPage = _services.GetRequiredService<SignUpPage>();

            Application.Current!.Windows[0].Page = new NavigationPage(signUpPage);
        }
    }
}
