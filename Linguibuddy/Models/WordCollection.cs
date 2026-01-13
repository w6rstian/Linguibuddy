using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Linguibuddy.Models
{
    public partial class WordCollection : ObservableObject
    {
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

        public List<CollectionItem> Items { get; set; } = [];

        // Audio Quiz
        public double AudioBestScore { get; set; }
        public double AudioLastScore { get; set; }

        // Image Quiz
        public double ImageBestScore { get; set; }
        public double ImageLastScore { get; set; }

        // Sentence Quiz
        public double SentenceBestScore { get; set; }
        public double SentenceLastScore { get; set; }

        // Speaking Quiz
        public double SpeakingBestScore { get; set; }
        public double SpeakingLastScore { get; set; }

        // Hangman
        public double HangmanBestScore { get; set; }
        public double HangmanLastScore { get; set; }
    }
}