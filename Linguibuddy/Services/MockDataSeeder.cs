using System.Diagnostics;
using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Services;

public class MockDataSeeder
{
    private readonly DataContext _context;
    private readonly IDictionaryApiService _dictionaryApi;
    private readonly IPexelsImageService _imageApi;

    private readonly List<string> _wordsToSeed = new()
    {
        "apple", "dog", "cat", "car", "book", "tree", "house",
        "sun", "moon", "computer", "mountain", "ocean", "coffee",
        "bicycle"
    };

    public MockDataSeeder(IDictionaryApiService dictionaryApi, IPexelsImageService imageApi, DataContext context)
    {
        _dictionaryApi = dictionaryApi;
        _imageApi = imageApi;
        _context = context;
    }

    public async Task SeedAsync()
    {
        var addedOrUpdatedCount = 0;

        foreach (var wordText in _wordsToSeed)
            try
            {
                var existingWord = await _context.DictionaryWords
                    .FirstOrDefaultAsync(w => w.Word.ToLower() == wordText);

                if (existingWord == null)
                {
                    existingWord = await _dictionaryApi.GetEnglishWordAsync(wordText);

                    await Task.Delay(100);
                }

                if (existingWord != null && string.IsNullOrEmpty(existingWord.ImageUrl))
                {
                    var imageUrl = await _imageApi.GetImageUrlAsync(wordText);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        existingWord.ImageUrl = imageUrl;

                        _context.DictionaryWords.Update(existingWord);
                        addedOrUpdatedCount++;

                        Debug.WriteLine($"[SEEDER] Pobrano zdjęcie dla: {wordText}");

                        await Task.Delay(250);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SEEDER ERROR] Błąd przy słowie {wordText}: {ex.Message}");
            }

        if (addedOrUpdatedCount > 0 || _context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync();
            Debug.WriteLine($"[SEEDER] Zakończono. Zaktualizowano {addedOrUpdatedCount} zdjęć/słów.");
        }
        else
        {
            Debug.WriteLine("[SEEDER] Wszystkie słowa są aktualne.");
        }
    }
}