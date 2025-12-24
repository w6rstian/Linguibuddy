using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Linguibuddy.Services
{
    public class PexelsImageService
    {
        private readonly HttpClient _httpClient;

        public PexelsImageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> GetImageUrlAsync(string word)
        {
            try
            {
                var url = $"search?query={word}&per_page=1&size=medium";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[Pexels API Error] Status: {response.StatusCode}");
                    return null;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(jsonString);
                var photos = data["photos"] as JArray;

                if (photos != null && photos.Count > 0)
                {
                    return photos[0]["src"]?["medium"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Pexels Exception] {ex.Message}");
            }

            return null;
        }
    }
}