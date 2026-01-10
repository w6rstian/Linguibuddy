using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Views;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Linguibuddy.ViewModels
{
    public partial class SignUpViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;
        private readonly DataContext db;

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

        public SignUpViewModel(FirebaseAuthClient authClient, IServiceProvider services, DataContext dataContext)
        {
            _authClient = authClient;
            _services = services;
            db = dataContext;
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

            // This check is excessive but I'd rather have it than not, for safety =)
            var appUser = await db.AppUsers.FindAsync(_authClient.User.Uid);
            if (appUser == null)
            {
                appUser = new AppUser
                {
                    Id = _authClient.User.Uid
                };
                db.AppUsers.Add(appUser);

                var allAchievements = await db.Achievements.ToListAsync(); // Pobierz wszystkie globalne osiągnięcia

                foreach (var achievement in allAchievements)
                {
                    var userAchievement = new UserAchievement
                    {
                        AppUserId = appUser.Id,
                        AchievementId = achievement.Id,
                        IsUnlocked = false
                    };
                    db.UserAchievements.Add(userAchievement);
                }

                await db.SaveChangesAsync();
            }

            Application.Current.Windows[0].Page = App.GetMainShell();
        }
        [RelayCommand]
        private async Task NavigateSignIn()
        {
            var signInPage = _services.GetRequiredService<SignInPage>();

            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
        }
    }
}
