namespace Linguibuddy.Interfaces;

public interface ILearningService
{
    Task MarkLearnedTodayAsync();
    Task<int> GetCurrentStreakAsync();
}
