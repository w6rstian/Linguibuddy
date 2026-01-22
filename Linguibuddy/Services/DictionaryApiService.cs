using System.Diagnostics;
using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Linguibuddy.Services;

public class DictionaryApiService : IDictionaryApiService
{
    private readonly DataContext _context;
    private readonly HttpClient _httpClient;
    private readonly IPexelsImageService _pexelsService;

    public DictionaryApiService(HttpClient httpClient, DataContext context, IPexelsImageService pexelsService)
    {
        _httpClient = httpClient;
        _context = context;
        _pexelsService = pexelsService;
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
            var response = await _httpClient.GetAsync(searchWord);

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[API ERROR] Kod: {response.StatusCode} dla słowa {searchWord}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<List<DictionaryWord>>(json);
            var fetchedWord = apiResponse?.FirstOrDefault();

            if (fetchedWord != null)
            {
                var perfectMatch = fetchedWord.Phonetics
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text) && !string.IsNullOrWhiteSpace(p.Audio));

                if (string.IsNullOrWhiteSpace(fetchedWord.Phonetic))
                {
                    if (perfectMatch != null)
                        fetchedWord.Phonetic = perfectMatch.Text;
                    else
                        fetchedWord.Phonetic = fetchedWord.Phonetics
                            .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text))?.Text ?? "";
                }

                fetchedWord.Audio = perfectMatch?.Audio
                                    ?? fetchedWord.Phonetics.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Audio))
                                        ?.Audio
                                    ?? "";

                //próba pobrania też zdjęcia do słowa od razu
                try
                {
                    var imageUrl = await _pexelsService.GetImageUrlAsync(fetchedWord.Word);
                    if (!string.IsNullOrEmpty(imageUrl)) fetchedWord.ImageUrl = imageUrl;
                }
                catch (Exception imgEx)
                {
                    Debug.WriteLine($"[PEXELS ERROR] Nie udało się pobrać zdjęcia: {imgEx.Message}");
                }

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
        catch (Exception ex)
        {
            Debug.WriteLine($"[API EXCEPTION] {ex.Message}");
        }

        return null;
    }

    // not used currently
    public async Task<List<DictionaryWord>> GetRandomWordsForGameAsync(int count = 4)
    {
        var validIds = await _context.DictionaryWords
            .Where(w => !string.IsNullOrEmpty(w.Audio) && !string.IsNullOrEmpty(w.Phonetic))
            .Select(w => w.Id)
            .ToListAsync();

        if (validIds.Count < count)
        {
            if (validIds.Count == 0) return new List<DictionaryWord>();

            count = validIds.Count;
        }

        var random = new Random();
        var selectedIds = validIds
            .OrderBy(x => random.Next())
            .Take(count)
            .ToList();

        var randomWords = await _context.DictionaryWords
            .Where(w => selectedIds.Contains(w.Id))
            .ToListAsync();

        return randomWords;
    }

    // not used currently
    public async Task<List<DictionaryWord>> GetRandomWordsWithImagesAsync(int count = 4)
    {
        var validIds = await _context.DictionaryWords
            .Where(w => !string.IsNullOrEmpty(w.Audio)
                        && !string.IsNullOrEmpty(w.Phonetic)
                        && !string.IsNullOrEmpty(w.ImageUrl))
            .Select(w => w.Id)
            .ToListAsync();

        if (validIds.Count < count)
        {
            if (validIds.Count == 0) return new List<DictionaryWord>();
            count = validIds.Count;
        }

        var random = new Random();
        var selectedIds = validIds
            .OrderBy(x => random.Next())
            .Take(count)
            .ToList();

        var randomWords = await _context.DictionaryWords
            .Where(w => selectedIds.Contains(w.Id))
            .ToListAsync();

        return randomWords;
    }
}