using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IUserLearningDayRepository
{
    Task<bool> ExistsAsync(string userId, DateTime date);
    Task AddAsync(UserLearningDay day);
    Task<List<DateTime>> GetLearningDatesAsync(string userId);
}