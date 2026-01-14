using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IDictionaryApiService
{
    Task<DictionaryWord?> GetEnglishWordAsync(string englishWord);
    Task<List<DictionaryWord>> GetRandomWordsForGameAsync(int count = 4);
    Task<List<DictionaryWord>> GetRandomWordsWithImagesAsync(int count = 4);
}
