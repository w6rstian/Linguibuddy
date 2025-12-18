using Linguibuddy.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Linguibuddy.Services
{
    public class MockDataSeeder
    {
        private readonly DictionaryApiService _apiService;
        private readonly DataContext _context;

        private readonly Dictionary<string, string> _wordsToSeed = new()
        {
            { "apple", "https://images.pexels.com/photos/102104/pexels-photo-102104.jpeg" },
            { "dog", "https://images.pexels.com/photos/1108099/pexels-photo-1108099.jpeg" },
            { "cat", "https://upload.wikimedia.org/wikipedia/commons/3/3a/Cat03.jpg" },
            { "car", "https://images.pexels.com/photos/170811/pexels-photo-170811.jpeg" },
            { "book", "https://images.pexels.com/photos/1516983/pexels-photo-1516983.jpeg" },
            { "tree", "https://images.pexels.com/photos/1459495/pexels-photo-1459495.jpeg" },
            { "house", "https://images.pexels.com/photos/106399/pexels-photo-106399.jpeg" },
            { "sun", "https://images.pexels.com/photos/301599/pexels-photo-301599.jpeg" },
            { "moon", "https://images.pexels.com/photos/47367/full-moon-moon-bright-sky-47367.jpeg" },
            { "computer", "https://images.pexels.com/photos/1779487/pexels-photo-1779487.jpeg" }
        };


        public MockDataSeeder(DictionaryApiService apiService, DataContext context)
        {
            _apiService = apiService;
            _context = context;
        }

        public async Task SeedAsync()
        {
            int addedCount = 0;

            foreach (var item in _wordsToSeed)
            {
                var wordText = item.Key;
                var imageUrl = item.Value;

                try
                {
                    var existingWord = await _context.DictionaryWords
                        .FirstOrDefaultAsync(w => w.Word.ToLower() == wordText);

                    if (existingWord == null)
                    {
                        existingWord = await _apiService.GetEnglishWordAsync(wordText);
                        addedCount++;
                    }

                    if (existingWord != null && string.IsNullOrEmpty(existingWord.ImageUrl))
                    {
                        existingWord.ImageUrl = imageUrl;

                        _context.DictionaryWords.Update(existingWord);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SEEDER ERROR] Nie udało się dodać słowa {wordText}: {ex.Message}");
                }
            }

            if (addedCount > 0 || _context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
                Debug.WriteLine($"[SEEDER] Zakończono mockowanie. Dodano/zaktualizowano słowa.");
            }
            else
            {
                Debug.WriteLine($"[SEEDER] Wszystkie słowa już istnieją w bazie.");
            }
        }
    }
}