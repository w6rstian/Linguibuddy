using System.Diagnostics;
using DeepL;
using Linguibuddy.Interfaces;

namespace Linguibuddy.Services;

public class DeepLTranslationService : IDeepLTranslationService
{
    private readonly IDeepLClientWrapper _client;

    public DeepLTranslationService(IDeepLClientWrapper client)
    {
        _client = client;
    }

    public async Task<string> TranslateWithContextAsync(string word, string definition, string partOfSpeech,
        string targetLang = "PL")
    {
        if (string.IsNullOrWhiteSpace(word)) return string.Empty;

        try
        {
            var contextString = $"{partOfSpeech}. {definition}";

            var options = new TextTranslateOptions
            {
                Context = contextString,
                Formality = Formality.Less,
                SentenceSplittingMode = SentenceSplittingMode.Off
            };
            var result = await _client.TranslateTextAsync(word, "EN", targetLang, options);

            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DeepL Error: {ex.Message}");
            return word;
        }
    }
}