using CommunityToolkit.Mvvm.ComponentModel;
using Firebase.Auth;
using Linguibuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly AppUserService _appUserService;
        private readonly LearningService _learningService;
        private readonly FirebaseAuthClient _authClient;
        private readonly AchievementService _achievementService;

        [ObservableProperty]
        private string _displayName;
        [ObservableProperty]
        private string _email;
        [ObservableProperty]
        private int _bestStreak;
        [ObservableProperty]
        private int _currentStreak;
        [ObservableProperty]
        private int _points;
        [ObservableProperty]
        private bool _isCurrentStreakBest = false;
        [ObservableProperty]
        private int _unlockedAchievementsCount;

        public ProfileViewModel(AppUserService appUserService, LearningService learningService, FirebaseAuthClient authClient, AchievementService achievementService)
        {
            _appUserService = appUserService;
            _learningService = learningService;
            _authClient = authClient;
            _achievementService = achievementService;
        }

        public async Task LoadProfileInfoAsync()
        {
            DisplayName = _authClient.User.Info.DisplayName;
            Email = _authClient.User.Info.Email;
            Points = await _appUserService.GetUserPointsAsync();
            CurrentStreak = await _learningService.GetCurrentStreakAsync();
            BestStreak = await _appUserService.GetUserBestStreakAsync();

            if (CurrentStreak == BestStreak)
                IsCurrentStreakBest = true;

            UnlockedAchievementsCount = await _achievementService.GetUnlockedAchievementsCountAsync();
        }
    }
}
