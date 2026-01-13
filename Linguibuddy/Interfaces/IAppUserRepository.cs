using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IAppUserRepository
{
    Task<AppUser?> GetByIdAsync(string userId);
    Task AddAsync(AppUser user);
    Task SaveChangesAsync();
}