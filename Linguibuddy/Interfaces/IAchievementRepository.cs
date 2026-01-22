using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IAchievementRepository
{
    Task<List<UserAchievement>> GetUserAchievementsAsync();
    Task<List<UserAchievement>> GetUserAchievementsAsNoTrackingAsync();
    Task<int> GetUnlockedAchievementsCountAsync();
}