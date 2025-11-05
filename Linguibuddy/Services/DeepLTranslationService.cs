using DeepL;

namespace Linguibuddy.Services
{
    public class DeepLTranslationService
    {
        private readonly DeepLClient _client;

        public DeepLTranslationService(string? apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("Missing DeepL API key. Please ensure you have a .env file with DEEPL_API_KEY set.");

            _client = new DeepLClient(apiKey);
        }

        public async Task<string> TranslateTextAsync(string text, string targetLang = "PL")
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            try
            {
                var result = await _client.TranslateTextAsync(text, null, targetLang);
                return result.Text;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Translation error: {ex.Message}");
                return $"[Translation error: {ex.Message}]";
            }
        }
    }
}
