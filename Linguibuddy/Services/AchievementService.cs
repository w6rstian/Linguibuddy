using Linguibuddy.Data;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Services
{
    public class AchievementService
    {
        private readonly DataContext _db;

        public AchievementService(DataContext db)
        {
            _db = db;
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string appUserId)
        {
            return await _db.UserAchievements
                .Include(ua => ua.Achievement) // Eager load detali osiągnięcia
                .Where(ua => ua.AppUserId == appUserId)
                .ToListAsync();
        }

        public async Task CheckAchievementsAsync(string appUserId, int achievementId, float progressIncrement)
        {
            // TODO: Jakiś syf trzeba zmienić
            var userAchievement = await _db.UserAchievements
                .FirstOrDefaultAsync(ua => ua.AppUserId == appUserId && ua.AchievementId == achievementId);

            if (userAchievement == null)
            {
                userAchievement = new UserAchievement { AppUserId = appUserId, AchievementId = achievementId, Progress = 0 };
                _db.UserAchievements.Add(userAchievement);
            }

            if (userAchievement.IsUnlocked) return;

            userAchievement.Progress += progressIncrement;

            if (userAchievement.Progress >= 100) // Example condition
            {
                userAchievement.IsUnlocked = true;
                userAchievement.UnlockDate = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            // Popup logic here...
        }
    }
}
