using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.ViewModels;

public partial class AchievementsViewModel : ObservableObject
{
    private readonly AchievementService _achievementService;

    [ObservableProperty] private ObservableCollection<UserAchievement> achievements = new(); // Lista do bindowania

    [ObservableProperty] private bool isLoading = true; // Do pokazywania loadera

    public AchievementsViewModel(AchievementService achievementService)
    {
        _achievementService = achievementService;
    }

    [RelayCommand]
    private async Task LoadAchievementsAsync()
    {
        IsLoading = true;
        Achievements.Clear();

        var userAchievements = await _achievementService.GetUserAchievementsAsync();

        foreach (var ua in userAchievements) Achievements.Add(ua);

        IsLoading = false;
    }
}