using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Linguibuddy.Models
{
    public class Flashcard
    {
        public int Id { get; set; }
        // [MaxLength(wartoœæ)] - tylko do odczytu w bazie danych, tylko maksymalna d³ugoœæ znaków
        // [StringLength(wartoœæ, ErrorMessage = "Komunikat o b³êdzie] - do odczytu i zapisu w bazie danych, maksymalna d³ugoœæ znaków z komunikatem o b³êdzie, mo¿liwoœæ dania minimalnej d³ugoœci
        // {0} = Property Name, {1} = Max Length, {2} = Min Length
        [StringLength(50, ErrorMessage = "{0} can have a max of {1} characters")]
        public string Word { get; set; } = string.Empty;
        [StringLength(128, ErrorMessage = "{0} can have a max of {1} characters")]
        public string Translation { get; set; } = string.Empty;
        [StringLength(50, ErrorMessage = "{0} can have a max of {1} characters")]
        public string PartOfSpeech { get; set; } = string.Empty;
        [StringLength(512, ErrorMessage = "{0} can have a max of {1} characters")]
        public string ExampleSentence { get; set; } = string.Empty;

        public int CollectionId { get; set; }

        [ForeignKey(nameof(CollectionId))]
        public FlashcardCollection? Collection { get; set; }
    }
}