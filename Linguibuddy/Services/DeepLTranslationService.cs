using DeepL;

namespace Linguibuddy.Services
{
    public class DeepLTranslationService
    {
        private readonly DeepLClient _client;

        public DeepLTranslationService(string? apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API key is required", nameof(apiKey));

            _client = new DeepLClient(apiKey);
        }

        public async Task<string> TranslateWithContextAsync(string word, string definition, string partOfSpeech, string targetLang = "PL")
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
                var result = await _client.TranslateTextAsync(word, sourceLanguageCode: "EN", targetLang, options);

                return result.Text;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DeepL Error: {ex.Message}");
                return word;
            }
        }
    }
}
