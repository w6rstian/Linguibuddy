using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Models;
using Linguibuddy.Services;
using System.Collections.ObjectModel;

namespace Linguibuddy.ViewModels
{
    public partial class AchievementsViewModel : ObservableObject
    {
        private readonly AchievementService _achievementService;
        private readonly string _currentUserId; // Zakładam, że masz dostęp do userId (np. z Firebase lub AppUser)
        private readonly FirebaseAuthClient _authClient;

        [ObservableProperty]
        private ObservableCollection<UserAchievement> achievements = new(); // Lista do bindowania

        [ObservableProperty]
        private bool isLoading = true; // Do pokazywania loadera

        public AchievementsViewModel(AchievementService achievementService, FirebaseAuthClient authClient)
        {
            _achievementService = achievementService;
            _authClient = authClient;
            _currentUserId = _authClient.User.Uid;
        }

        [RelayCommand]
        private async Task LoadAchievementsAsync()
        {
            IsLoading = true;
            Achievements.Clear();

            var userAchievements = await _achievementService.GetUserAchievementsAsync(_currentUserId);

            foreach (var ua in userAchievements)
            {
                Achievements.Add(ua);
            }

            IsLoading = false;
        }
    }
}

