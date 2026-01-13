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
    public class UserLearningDayRepository : IUserLearningDayRepository
    {
        private readonly DataContext _db;

        public UserLearningDayRepository(DataContext db)
        {
            _db = db;
        }

        public async Task<bool> ExistsAsync(string userId, DateTime date)
            => await _db.UserLearningDays
            .AnyAsync(uld => uld.AppUserId == userId && uld.Date == date);

        public async Task AddAsync(UserLearningDay day)
        {
            await _db.UserLearningDays.AddAsync(day);
            await _db.SaveChangesAsync();
        }

        public async Task<List<DateTime>> GetLearningDatesAsync(string userId)
            => await _db.UserLearningDays
            .Where(uld => uld.AppUserId == userId)
            .OrderByDescending(uld => uld.Date)
            .Select(uld => uld.Date)
            .ToListAsync();
    }
}
