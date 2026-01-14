using CommunityToolkit.Mvvm.ComponentModel;
using Firebase.Auth;
using Linguibuddy.Interfaces;
using Linguibuddy.Services;
using Linguibuddy.Views;

namespace Linguibuddy.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IAppUserService _appUserService;
    private readonly FirebaseAuthClient _authClient;
    private readonly ILearningService _learningService;

    [ObservableProperty] private int _bestStreak;

    [ObservableProperty] private int _currentStreak;

    [ObservableProperty] private string _displayName;

    [ObservableProperty] private string _email;

    [ObservableProperty] private bool _isCurrentStreakBest;

    [ObservableProperty] private int _points;

    [ObservableProperty] private int _unlockedAchievementsCount;

    public ProfileViewModel(IAppUserService appUserService, ILearningService learningService,
        FirebaseAuthClient authClient, IAchievementRepository achievementRepository)
    {
        _appUserService = appUserService;
        _learningService = learningService;
        _authClient = authClient;
        _achievementRepository = achievementRepository;
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

        UnlockedAchievementsCount = await _achievementRepository.GetUnlockedAchievementsCountAsync();
    }
}