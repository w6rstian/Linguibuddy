using Linguibuddy.Models;
using System.Net.Http.Json;

namespace Linguibuddy.Services
{
    public class DictionaryApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://freedictionaryapi.com/api/v1/";

        public DictionaryApiService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
        }

        public async Task<List<string>> GetPolishTranslationsAsync(string englishWord)
        {
            if (string.IsNullOrWhiteSpace(englishWord))
                return [];

            try
            {
                var url = $"entries/en/{englishWord}?translations=true";
                var result = await _httpClient.GetFromJsonAsync<DictionaryResponse>(url);

                if (result?.Entries == null)
                    return [];

                var translations = result.Entries
                    .SelectMany(e => e.Senses ?? [])
                    .Where(s => s.Translations != null)
                    .SelectMany(s => s.Translations)
                    .Where(t => t.Language.Code.Equals("pl", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.Word)
                    .Distinct()
                    .ToList();

                return translations;
            }
            catch (HttpRequestException e)
            {
                throw new Exception($"Connection error: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Api error: {e.Message}");
                return [];
            }
        }
    }
}
