using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Repositories;

public class AchievementRepository : IAchievementRepository
{
    private readonly IAuthService _authService;
    private readonly string _currentUserId;
    private readonly DataContext _db;

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