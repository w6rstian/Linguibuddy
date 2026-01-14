using Linguibuddy.Helpers;
using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IScoringService
{
    int CalculatePoints(GameType gameType, DifficultyLevel difficulty);
    Task SaveResultsAsync(WordCollection collection, GameType gameType, int correctAnswers, int totalQuestions,
        int totalPointsEarned);
}
