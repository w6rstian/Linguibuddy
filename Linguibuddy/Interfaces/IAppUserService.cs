using Linguibuddy.Helpers;
using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IAppUserService
{
    Task AddUserPointsAsync(int points);
    Task<int> GetUserPointsAsync();
    Task<DifficultyLevel> GetUserDifficultyAsync();
    Task SetUserDifficultyAsync(DifficultyLevel level);
    Task<int> GetUserLessonLengthAsync();
    Task SetUserLessonLengthAsync(int length);
    Task<int> GetUserBestStreakAsync();
    Task SetBestLearningStreakAsync(int newStreak);
}
