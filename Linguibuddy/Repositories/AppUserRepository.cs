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
    public class AppUserRepository : IAppUserRepository
    {
        private readonly DataContext _db;

        public AppUserRepository(DataContext db)
        {
            _db = db;
        }

        public async Task<AppUser?> GetByIdAsync(string userId)
            => await _db.AppUsers
            .Where(au => au.Id == userId)
            .FirstAsync();

        public async Task AddAsync(AppUser user)
        {
            await _db.AppUsers.AddAsync(user);
            await _db.SaveChangesAsync();
        }


        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
