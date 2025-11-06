using Linguibuddy.Models;
using Newtonsoft.Json;

namespace Linguibuddy.Services
{
    public class DictionaryApiService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.dictionaryapi.dev/api/v2/entries/en/";

        public DictionaryApiService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
        }

        public async Task<WordEntry?> GetEnglishWordAsync(string englishWord)
        {
            if (string.IsNullOrWhiteSpace(englishWord))
                return null;

            try
            {
                var url = $"{BaseUrl}{englishWord}";
                var json = await _httpClient.GetStringAsync(url);
                var response = JsonConvert.DeserializeObject<List<WordEntry>>(json);
                return response?.FirstOrDefault();
            }
            catch (HttpRequestException e)
            {
                throw new Exception($"Connection error: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Api error: {e.Message}");
                return null;
            }
        }
    }
}
