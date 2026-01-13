using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Services
{
    public class CollectionService
    {
        private readonly DataContext _context;
        private readonly FirebaseAuthClient _authClient;

        public CollectionService(DataContext context, FirebaseAuthClient authClient)
        {
            _context = context;
            _authClient = authClient;
        }

        private string GetUserId()
        {
            var uid = _authClient.User?.Uid;
            if (string.IsNullOrEmpty(uid))
                throw new UnauthorizedAccessException("Użytkownik nie jest zalogowany.");
            return uid;
        }

        public async Task<List<WordCollection>> GetUserCollectionsAsync()
        {
            var userId = GetUserId();
            return await _context.WordCollections
                .Where(c => c.UserId == userId)
                .Include(c => c.Items)
                .ToListAsync();
        }

        public async Task<WordCollection?> GetCollection(int id)
        {
            return await _context.WordCollections
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task CreateCollectionAsync(string name)
        {
            var userId = GetUserId();
            var newCollection = new WordCollection { Name = name, UserId = userId };
            _context.WordCollections.Add(newCollection);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCollectionAsync(WordCollection collection)
        {
            _context.WordCollections.Update(collection);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCollectionAsync(WordCollection collection)
        {
            _context.WordCollections.Remove(collection);
            await _context.SaveChangesAsync();
        }

        public async Task RenameCollectionAsync(WordCollection collection, string newName)
        {
            collection.Name = newName;
            await UpdateCollectionAsync(collection);
        }

        public async Task<List<CollectionItem>> GetItemsForLearning(int collectionId)
        {
            return await _context.CollectionItems
                .Include(i => i.FlashcardProgress)
                .Where(i => i.CollectionId == collectionId)
                .OrderBy(i => i.AddedDate)
                .ToListAsync();
        }

        // pobranie fiszek do powtórki na dziś
        public async Task<List<CollectionItem>> GetItemsDueForLearning(int collectionId)
        {
            var today = DateTime.UtcNow;

            return await _context.CollectionItems
                .Include(i => i.FlashcardProgress)
                .Where(i => i.CollectionId == collectionId
                            && i.FlashcardProgress != null
                            && i.FlashcardProgress.NextReviewDate <= today)
                .OrderBy(i => i.FlashcardProgress.NextReviewDate)
                .ToListAsync();
        }

        public async Task UpdateFlashcardProgress(Flashcard flashcard)
        {
            _context.Set<Flashcard>().Update(flashcard);
            await _context.SaveChangesAsync();
        }

        public async Task AddCollectionItemFromDtoAsync(int collectionId, FlashcardCreationDto dto)
        {
            bool exists = await _context.CollectionItems.AnyAsync(i =>
                i.CollectionId == collectionId &&
                i.Word.ToLower() == dto.Word.ToLower() &&
                i.PartOfSpeech == dto.PartOfSpeech &&
                i.Definition == dto.Definition
            );

            if (exists)
                return;

            var newItem = new CollectionItem
            {
                CollectionId = collectionId,

                Word = dto.Word,
                Phonetic = dto.Phonetic,
                Audio = dto.Audio,
                ImageUrl = dto.ImageUrl,

                PartOfSpeech = dto.PartOfSpeech,
                Definition = dto.Definition,
                Example = dto.Example,
                SavedTranslation = dto.Translation,

                AddedDate = DateTime.UtcNow,

                // SRS init
                FlashcardProgress = new Flashcard
                {
                    NextReviewDate = DateTime.UtcNow,
                    Interval = 0,
                    Repetitions = 0,
                    EaseFactor = 2.5
                }
            };

            _context.CollectionItems.Add(newItem);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> GetWordsWithImagesFromCollectionAsync(int collectionId)
        {
            return await _context.CollectionItems
                .Where(i => i.CollectionId == collectionId
                         && !string.IsNullOrEmpty(i.ImageUrl))
                .Select(i => i.ImageUrl!)
                .ToListAsync();
        }
    }
}