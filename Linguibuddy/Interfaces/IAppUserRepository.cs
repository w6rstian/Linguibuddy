using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IAppUserRepository
{
    Task<AppUser?> GetByIdAsync(string userId);
    Task AddAsync(AppUser user);
    void Update(AppUser user);
    Task SaveChangesAsync();
    Task<List<AppUser>> GetTopUsersAsync(int count);
}