using Linguibuddy.Helpers;
using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IOpenAiService
{
    Task<string> TestConnectionAsync();
    Task<string> TranslateWithContextAsync(string word, string definition, string partOfSpeech);

    Task<(string English, string Polish)?> GenerateSentenceAsync(string targetWord, string difficultyLevel,
        string definition = "");

    Task<string> AnalyzeCollectionProgressAsync(WordCollection collection, DifficultyLevel userDifficulty,
        string language);

    Task<string> AnalyzeComprehensiveProfileAsync(AppUser user, int currentStreak, int unlockedAchievements,
        IEnumerable<WordCollection> collections, string language);
}