using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Repositories;

public class AppUserRepository : IAppUserRepository
{
    private readonly DataContext _db;

    public AppUserRepository(DataContext db)
    {
        _db = db;
    }

    public async Task<AppUser?> GetByIdAsync(string userId)
    {
        return await _db.AppUsers
            .Where(au => au.Id == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AppUser>> GetTopUsersAsync(int count)
    {
        return await _db.AppUsers
            .OrderByDescending(u => u.Points)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(AppUser user)
    {
        await _db.AppUsers.AddAsync(user);
        await _db.SaveChangesAsync();
    }

    public void Update(AppUser user)
    {
        _db.AppUsers.Update(user);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}