using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Linguibuddy.Models
{
    public partial class WordCollection : ObservableObject
    {
        private string name = string.Empty;

        [Key]
        public int Id { get; set; }

        private string _name = string.Empty;

        [Required]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string? UserId { get; set; } = string.Empty;

        public List<CollectionItem> Items { get; set; } = new();
    }
}