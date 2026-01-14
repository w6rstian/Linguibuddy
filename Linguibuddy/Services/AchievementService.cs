using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Services;

public class AchievementService : IAchievementService
{
    private readonly FirebaseAuthClient _authClient;
    private readonly IAppUserService _appUserService;
    private readonly IAchievementRepository _achievementRepository;

    private readonly string _currentUserId;
    private readonly DataContext _db;

    public AchievementService(
        DataContext db, 
        FirebaseAuthClient authClient, 
        IAppUserService appUserService, 
        IAchievementRepository achievementRepository
        )
    {
        _db = db;
        _authClient = authClient;
        _currentUserId = _authClient.User.Uid;
        _appUserService = appUserService;
        _achievementRepository = achievementRepository;
    }

    public async Task CheckAchievementsAsync()
    {
        var userAchievements = await _achievementRepository.GetUserAchievementsAsync();

        if (userAchievements == null || !userAchievements.Any())
        {
            return;
        }

        var userPoints = await _appUserService.GetUserPointsAsync();
        var userStreak = await _appUserService.GetUserBestStreakAsync();

        foreach (var userAchievement in userAchievements)
        {
            var correspondingAchievement = userAchievement.Achievement;
            switch (correspondingAchievement.UnlockCondition)
            {
                case AchievementUnlockType.TotalPoints:
                    if (correspondingAchievement.UnlockTargetValue <= userPoints)
                    {
                        userAchievement.IsUnlocked = true;
                        userAchievement.UnlockDate = DateTime.Today;
                    }
                    break;

                case AchievementUnlockType.LearningStreak:
                    if (correspondingAchievement.UnlockTargetValue <= userStreak)
                    {
                        userAchievement.IsUnlocked = true;
                        userAchievement.UnlockDate = DateTime.Today;
                    }    
                    break;

                default:
                    break;
            }
        }

        await _db.SaveChangesAsync();
    }
}