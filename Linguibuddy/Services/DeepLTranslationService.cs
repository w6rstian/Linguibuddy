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

        public async Task<string> TranslateTextAsync(string text, string targetLang = "PL", string? partOfSpeech = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            try
            {
                string contextualText = partOfSpeech?.ToLower() switch
                {
                    "verb" => $"to {text}",
                    "noun" => $"the {text}",
                    "adjective" => $"something {text}",
                    _ => text
                };

                var options = new TextTranslateOptions
                {
                    SentenceSplittingMode = SentenceSplittingMode.Off, // nie dzieli na zdania
                    PreserveFormatting = true,
                    Formality = Formality.Default,
                    Context = "Translate this as a dictionary entry for language learners, focusing on the most common Polish meaning."
                };

                var result = await _client.TranslateTextAsync(contextualText, null, targetLang, options);

                var translated = result.Text
                    .Replace("to ", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("the ", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("something ", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();

                if (translated.Equals(text, StringComparison.OrdinalIgnoreCase))
                    return text;

                return translated;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Translation error: {ex.Message}");
                return $"[Translation error: {ex.Message}]";
            }
        }
    }
}
