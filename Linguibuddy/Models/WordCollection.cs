using System.ComponentModel.DataAnnotations;

namespace Linguibuddy.Models
{
    public class WordCollection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? UserId { get; set; } = string.Empty;

        public List<CollectionItem> Items { get; set; } = new();
    }
}