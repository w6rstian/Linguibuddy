using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace Linguibuddy.Models
{
    public class CollectionItem
    {
        [Key]
        public int Id { get; set; }

        public int CollectionId { get; set; }

        [ForeignKey(nameof(CollectionId))]
        public WordCollection? Collection { get; set; }

        [Required]
        public string Word { get; set; } = string.Empty;

        public string Phonetic { get; set; } = string.Empty;
        public string Audio { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public string PartOfSpeech { get; set; } = string.Empty;

        public int DefinitionId { get; set; }

        public string Definition { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;

        public string Context { get; set; } = string.Empty;

        public string SavedTranslation { get; set; } = string.Empty;


        public bool IsLearned { get; set; } = false;

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    }
}