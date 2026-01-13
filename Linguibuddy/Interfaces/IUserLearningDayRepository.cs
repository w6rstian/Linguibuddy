using Linguibuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Interfaces
{
    public interface IUserLearningDayRepository
    {
        Task<bool> ExistsAsync(string userId, DateTime date);
        Task AddAsync(UserLearningDay day);
        Task<List<DateTime>> GetLearningDatesAsync(string userId);
    }
}
