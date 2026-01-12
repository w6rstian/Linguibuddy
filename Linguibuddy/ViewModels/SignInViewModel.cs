using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Views;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.ViewModels
{
    public partial class SignInViewModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;
        private readonly DataContext db;
        private readonly SettingsViewModel _settingsViewModel;

        [ObservableProperty]
        private string _email;
        [ObservableProperty]
        private string _password;
        [ObservableProperty]
        private float _labelErrorOpacity;

        public SignInViewModel(FirebaseAuthClient authClient, IServiceProvider services, DataContext dataContext, SettingsViewModel settingsViewModel)
        {
            _authClient = authClient;
            _services = services;
            _settingsViewModel = settingsViewModel;
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

                var allAchievements = await db.Achievements.ToListAsync(); // Pobierz wszystkie globalne osiągnięcia
                var userAchievements = await db.UserAchievements
                    .Where(u => u.AppUserId == appUser.Id)
                    .ToListAsync();

                foreach (var achievement in allAchievements)
                {
                    if (!userAchievements.Exists(ua => ua.AchievementId == achievement.Id))
                    {
                        var userAchievement = new UserAchievement
                        {
                            AppUserId = appUser.Id,
                            AchievementId = achievement.Id,
                            IsUnlocked = false
                        };
                        db.UserAchievements.Add(userAchievement);
                    }
                }

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
