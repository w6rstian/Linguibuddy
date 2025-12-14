using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;

namespace Linguibuddy.ViewModels
{
    public partial class FlashcardsViewModel : ObservableObject
    {
        private Queue<Flashcard> _flashcardsQueue;

        [ObservableProperty]
        private Flashcard _currentFlashcard;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLearning))]
        private bool _isFinished;

        public bool IsLearning => !IsFinished;

        public FlashcardsViewModel()
        {
            LoadData();
            NextCard();
        }

        private void LoadData()
        {
            // Przyk³adowe dane
            var data = new List<Flashcard>
            {
                new Flashcard { Word = "Serendipity", Translation = "Szczêœliwy traf", PartOfSpeech = "noun", ExampleSentence = "It was pure serendipity that we met." },
                new Flashcard { Word = "To develop", Translation = "Rozwijaæ (siê)", PartOfSpeech = "verb", ExampleSentence = "He wants to develop his skills." },
                new Flashcard { Word = "Resilient", Translation = "Odporny", PartOfSpeech = "adjective", ExampleSentence = "Bamboo is a resilient material." }
            };

            _flashcardsQueue = new Queue<Flashcard>(data);
        }

        [RelayCommand]
        public void NextCard()
        {
            if (_flashcardsQueue.Count > 0)
            {
                CurrentFlashcard = _flashcardsQueue.Dequeue();
                IsFinished = false;
            }
            else
            {
                CurrentFlashcard = null;
                IsFinished = true;
            }
        }
    }
}