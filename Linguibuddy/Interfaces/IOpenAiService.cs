namespace Linguibuddy.Interfaces;

public interface IOpenAiService
{
    Task<string> TestConnectionAsync();
    Task<string> TranslateWithContextAsync(string word, string definition, string partOfSpeech);
    Task<(string English, string Polish)?> GenerateSentenceAsync(string targetWord, string difficultyLevel);
}
