using DeepL;

namespace Linguibuddy.Interfaces;

public interface IDeepLClientWrapper
{
    Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang, TextTranslateOptions? options = null);
}
