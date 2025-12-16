using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.ViewModels
{
    [QueryProperty(nameof(Collection), "Collection")]
    public partial class FlashcardsViewModel : ObservableObject
    {
        private readonly FlashcardService _flashcardService;
        private Queue<Flashcard> _flashcardsQueue = new();

        [ObservableProperty]
        private FlashcardCollection? _collection;

        [ObservableProperty]
        private Flashcard? _currentFlashcard;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLearning))]
        private bool _isFinished;

        public bool IsLearning => !IsFinished;

        public FlashcardsViewModel(FlashcardService flashcardService)
        {
            _flashcardService = flashcardService;
        }

        async partial void OnCollectionChanged(FlashcardCollection? value)
        {
            if (value != null)
            {
                await StartLearning(value);
            }
        }

        private async Task StartLearning(FlashcardCollection collection)
        {
            var cards = await _flashcardService.GetFlashcardsForCollection(collection.Id);

            if (cards.Count == 0)
            {
                IsFinished = true;
                return;
            }

            var shuffled = cards.OrderBy(a => Guid.NewGuid()).ToList();
            _flashcardsQueue = new Queue<Flashcard>(shuffled);

            IsFinished = false;
            NextCard();
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

        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}