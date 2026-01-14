using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.ViewModels;

public partial class AchievementsViewModel : ObservableObject
{
    private readonly IAchievementService _achievementService;
    private readonly IAchievementRepository _achievementRepository;
    private ObservableCollection<UserAchievement> _allAchievements;

    [ObservableProperty] private ObservableCollection<UserAchievement> achievements = new(); // Lista do bindowania

    [ObservableProperty] private bool isLoading = true; // Do pokazywania loadera

    public AchievementsViewModel(IAchievementService achievementService, IAchievementRepository achievementRepository)
    {
        _achievementService = achievementService;
        _achievementRepository = achievementRepository;
    }

    [RelayCommand]
    private async Task LoadAchievementsAsync()
    {
        IsLoading = true;
        Achievements.Clear();
        await _achievementService.CheckAchievementsAsync();
        var _allAchievements = await _achievementRepository.GetUserAchievementsAsNoTrackingAsync();

        bool wasLastUnlocked = true;
        AchievementUnlockType lastType = AchievementUnlockType.TotalPoints;
        bool isNewType = false;
        foreach (var achievement in _allAchievements)
        {
            if (achievement.Achievement.UnlockCondition != lastType)
                isNewType = true;
            else
                isNewType = false;

            if (achievement.IsUnlocked)
            {
                wasLastUnlocked = true;
                Achievements.Add(achievement);
            }
            else if (wasLastUnlocked)
            {
                wasLastUnlocked = false;
                Achievements.Add(achievement);
            }
            else if (isNewType)
            {
                Achievements.Add(achievement);
            }
            else
            {
                continue;
            }
        }

        IsLoading = false;
    }
}