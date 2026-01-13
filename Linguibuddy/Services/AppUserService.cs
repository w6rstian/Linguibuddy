using Linguibuddy.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Services
{
    public class AppUserService
    {
        private readonly IAppUserRepository _appUsers;

        public AppUserService(IAppUserRepository appUsers)
        {
            _appUsers = appUsers;
        }

        public async Task AddPointsAsync(string userId, int points)
        {
            var user = await _appUsers.GetByIdAsync(userId)
                ?? throw new Exception("User not found");

            user.Points += points;

            await _appUsers.SaveChangesAsync();
        }

        //public async Task SetBestLearningStreakAsync(string userId, int )
        //public async Task<int> GetUserPoints(string userId);
    }
}
