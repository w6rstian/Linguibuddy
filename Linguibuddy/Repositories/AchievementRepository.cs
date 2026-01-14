using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Repositories
{
    public class AchievementRepository : IAchievementRepository
    {
        private readonly DataContext _db;
        private readonly FirebaseAuthClient _authClient;
        private string _currentUserId;

        public AchievementRepository(DataContext db, FirebaseAuthClient authClient)
        {
            _db = db;
            _authClient = authClient;
            _currentUserId = authClient.User.Uid;
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsync()
        {
            return await _db.UserAchievements
                .Include(ua => ua.Achievement) // Eager load detali osiągnięcia
                .Where(ua => ua.AppUserId == _currentUserId)
                .ToListAsync();
        }

        public async Task<List<UserAchievement>> GetUserAchievementsAsNoTrackingAsync()
        {
            return await _db.UserAchievements
                .Include(ua => ua.Achievement) // Eager load detali osiągnięcia
                .Where(ua => ua.AppUserId == _currentUserId)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<int> GetUnlockedAchievementsCountAsync()
        {
            return await _db.UserAchievements
                .Where(ua => ua.AppUserId == _currentUserId && ua.IsUnlocked)
                .AsNoTracking()
                .CountAsync();
        }
    }
}
