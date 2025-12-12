using Linguibuddy.Data;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Linguibuddy.Services
{
    public class DictionaryApiService
    {
        private readonly HttpClient _httpClient;
        private readonly DataContext _context;
        private const string BaseUrl = "https://api.dictionaryapi.dev/api/v2/entries/en/";

        public DictionaryApiService(DataContext context)
        {
            _context = context;

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
        }

        public async Task<DictionaryWord?> GetEnglishWordAsync(string englishWord)
       {
            if (string.IsNullOrWhiteSpace(englishWord))
                return null;

            var searchWord = englishWord.Trim().ToLower();

            try
            {
                var localWord = await _context.DictionaryWords
                    .Include(w => w.Phonetics)
                    .Include(w => w.Meanings)
                        .ThenInclude(m => m.Definitions)
                    .FirstOrDefaultAsync(w => w.Word.ToLower() == searchWord);

                if (localWord != null)
                {
                    Debug.WriteLine($"[CACHE] Słowo '{searchWord}' znalezione w lokalnej bazie.");
                    return localWord;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DB ERROR] Nie udało się odczytać z bazy: {ex.Message}");
            }

            try
            {
                var url = $"{BaseUrl}{englishWord}";
                var json = await _httpClient.GetStringAsync(url);
                var response = JsonConvert.DeserializeObject<List<DictionaryWord>>(json);

                var fetchedWord = response?.FirstOrDefault();

                if (fetchedWord != null)
                {
                    try
                    {
                        _context.DictionaryWords.Add(fetchedWord);

                        await _context.SaveChangesAsync();

                        Debug.WriteLine($"[API] Pobrano i zapisano słowo '{fetchedWord.Word}' do bazy.");
                    }
                    catch (Exception dbEx)
                    {
                        Debug.WriteLine($"[DB SAVE ERROR] Nie udało się zapisać słowa: {dbEx.Message}");
                    }
                }

                return fetchedWord;
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine($"[API ERROR] Błąd połączenia: {e.Message}");
                throw new Exception($"Nie znaleziono słowa lokalnie, a wystąpił błąd sieci: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Api error: {e.Message}");
                return null;
            }
        }
    }
}