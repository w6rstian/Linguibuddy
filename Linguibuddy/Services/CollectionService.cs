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
                .ThenInclude(i => i.DictionaryWord)
                .ToListAsync();
        }

        public async Task CreateCollectionAsync(string name)
        {
            var userId = GetUserId();
            var newCollection = new WordCollection { Name = name, UserId = userId };
            _context.WordCollections.Add(newCollection);
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
            _context.WordCollections.Update(collection);
            await _context.SaveChangesAsync();
        }

        public async Task AddWordToCollectionAsync(int collectionId, DictionaryWord word)
        {
            var dbWord = await _context.DictionaryWords.FirstOrDefaultAsync(w => w.Word == word.Word);

            if (dbWord == null)
            {
                return;
            }

            var exists = await _context.CollectionItems
                .AnyAsync(ci => ci.CollectionId == collectionId && ci.DictionaryWordId == dbWord.Id);

            if (exists) return;

            var newItem = new CollectionItem
            {
                CollectionId = collectionId,
                DictionaryWordId = dbWord.Id
            };

            _context.CollectionItems.Add(newItem);
            await _context.SaveChangesAsync();
        }

        public async Task<List<CollectionItem>> GetItemsForLearning(int collectionId)
        {
            return await _context.CollectionItems
               .Where(i => i.CollectionId == collectionId)
               .Include(i => i.DictionaryWord)
               .ThenInclude(dw => dw.Meanings)
               .ThenInclude(m => m.Definitions)
               .Include(i => i.DictionaryWord)
               .ThenInclude(dw => dw.Phonetics)
               .ToListAsync();
        }

        public async Task AddFlashcardFromDtoAsync(int collectionId, Linguibuddy.Models.FlashcardCreationDto dto)
        {
            var dbWord = await _context.DictionaryWords
                .FirstOrDefaultAsync(w => w.Word.ToLower() == dto.ParentWord.Word.ToLower());

            if (dbWord == null)
            {
                //// Słowa nie ma w bazie -> Dodajemy cały obiekt pobrany z API
                //// Ważne: dto.ParentWord musi być pełnym obiektem DictionaryWord
                //_context.DictionaryWords.Add(dto.ParentWord);
                //await _context.SaveChangesAsync();
                //dbWord = dto.ParentWord; // Teraz dbWord ma już nadane ID z bazy
                System.Diagnostics.Debug.WriteLine("Słowa nie ma w bazie");
                throw new Exception("Słowa nie ma w bazie");

            }

            var newItem = new CollectionItem
            {
                CollectionId = collectionId,
                DictionaryWordId = dbWord.Id,

                Context = dto.PartOfSpeech,
                SavedDefinition = dto.Definition,
                SavedTranslation = dto.Translation,
                SavedExample = dto.Example,

                IsLearned = false,
                AddedDate = DateTime.UtcNow
            };

            _context.CollectionItems.Add(newItem);
            await _context.SaveChangesAsync();
        }

    }
}