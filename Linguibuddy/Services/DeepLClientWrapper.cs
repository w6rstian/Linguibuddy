using DeepL;
using Linguibuddy.Interfaces;

namespace Linguibuddy.Services;

public class DeepLClientWrapper : IDeepLClientWrapper
{
    private readonly DeepLClient _client;

    public DeepLClientWrapper(DeepLClient client)
    {
        _client = client;
    }

    public async Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang, TextTranslateOptions? options = null)
    {
        var result = await _client.TranslateTextAsync(text, sourceLang, targetLang, options);
        return result.Text;
    }
}
