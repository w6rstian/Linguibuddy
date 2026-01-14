using Linguibuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Interfaces
{
    public interface IAchievementRepository
    {
        Task<List<UserAchievement>> GetUserAchievementsAsync();
        Task<List<UserAchievement>> GetUserAchievementsAsNoTrackingAsync();
        Task<int> GetUnlockedAchievementsCountAsync();
    }
}
