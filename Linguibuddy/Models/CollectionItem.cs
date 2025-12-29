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

        public int DictionaryWordId { get; set; }

        [ForeignKey(nameof(DictionaryWordId))]
        public DictionaryWord? DictionaryWord { get; set; }

        public string Context { get; set; } = string.Empty;

        public string SavedDefinition { get; set; } = string.Empty;

        public string SavedTranslation { get; set; } = string.Empty;

        public string SavedExample { get; set; } = string.Empty;

        public bool IsLearned { get; set; } = false;

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    }
}