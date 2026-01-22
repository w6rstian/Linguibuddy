namespace Linguibuddy.Interfaces;

public interface IDeepLTranslationService
{
    Task<string> TranslateWithContextAsync(string word, string definition, string partOfSpeech,
        string targetLang = "PL");
}