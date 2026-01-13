using Linguibuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Interfaces
{
    public interface IAppUserRepository
    {
        Task<AppUser?> GetByIdAsync(string userId);
        Task AddAsync(AppUser user);
        Task SaveChangesAsync();
    }
}
