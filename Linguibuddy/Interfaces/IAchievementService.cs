using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IAchievementService
{
    Task<List<UserAchievement>> GetUserAchievementsAsync();
    Task<int> GetUnlockedAchievementsCountAsync();
    Task CheckAchievementsAsync(string appUserId, int achievementId, float progressIncrement);
}
