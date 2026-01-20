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
        private readonly IAuthService _authService;
        private readonly string _currentUserId;

        public AchievementRepository(DataContext db, IAuthService authService)
        {
            _db = db;
            _authService = authService;
            _currentUserId = authService.CurrentUserId;
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
