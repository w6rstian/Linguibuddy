using Firebase.Auth;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linguibuddy.Models
{
    public class FlashcardCollection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public List<Flashcard> Flashcards { get; set; } = [];

        public string? UserId { get; set; } = String.Empty;
    }
}
