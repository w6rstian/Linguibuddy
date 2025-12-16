using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Services
{
    public class FlashcardService
    {
        private readonly DataContext _context;
        private readonly FirebaseAuthClient _authClient;

        public FlashcardService(DataContext context, FirebaseAuthClient authClient)
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

        public async Task<List<FlashcardCollection>> GetUserCollectionsAsync()
        {
            var userId = GetUserId();
            return await _context.FlashcardCollections
                .Where(c => c.UserId == userId)
                .Include(c => c.Flashcards)
                .ToListAsync();
        }

        public async Task CreateCollectionAsync(string name)
        {
            var userId = GetUserId();
            var newCollection = new FlashcardCollection
            {
                Name = name,
                UserId = userId
            };

            _context.FlashcardCollections.Add(newCollection);
            await _context.SaveChangesAsync();
        }

        public async Task AddFlashcardAsync(Flashcard flashcard)
        {
            _context.Flashcards.Add(flashcard);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Flashcard>> GetFlashcardsForCollection(int collectionId)
        {
            return await _context.Flashcards
               .Where(f => f.CollectionId == collectionId)
               .ToListAsync();
        }
    }
}