using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface ICollectionService
{
    Task<List<WordCollection>> GetUserCollectionsAsync();
    Task<WordCollection?> GetCollection(int id);
    Task CreateCollectionAsync(string name);
    Task UpdateCollectionAsync(WordCollection collection);
    Task DeleteCollectionAsync(WordCollection collection);
    Task RenameCollectionAsync(WordCollection collection, string newName);
    Task<List<CollectionItem>> GetItemsForLearning(int collectionId);
    Task<List<CollectionItem>> GetItemsDueForLearning(int collectionId);
    Task UpdateFlashcardProgress(Flashcard flashcard);
    Task<bool> AddCollectionItemFromDtoAsync(int collectionId, FlashcardCreationDto dto);
    Task DeleteCollectionItemAsync(CollectionItem item);
}
