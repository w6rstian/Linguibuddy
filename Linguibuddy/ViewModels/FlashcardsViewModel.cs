using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.ViewModels
{
    [QueryProperty(nameof(Collection), "Collection")]
    public partial class FlashcardsViewModel : ObservableObject
    {
        private readonly CollectionService _collectionService;
        private Queue<CollectionItem> _itemsQueue = new();

        [ObservableProperty]
        private WordCollection? _collection;

        [ObservableProperty]
        private CollectionItem? _currentItem;

        public string CurrentWordText => _currentItem?.DictionaryWord?.Word ?? "";

        public string CurrentPhonetic => _currentItem?.DictionaryWord?.Phonetic ?? "";

        public string CurrentAudio => _currentItem?.DictionaryWord?.Audio ?? "";

        public string CurrentImageUrl => _currentItem?.DictionaryWord?.ImageUrl ?? "";

        public string CurrentTranslation =>
            !string.IsNullOrEmpty(_currentItem?.SavedTranslation)
                ? _currentItem.SavedTranslation
                : "Brak t³umaczenia";

        public string CurrentDefinition
        {
            get
            {
                if (_currentItem == null) return "";

                if (!string.IsNullOrEmpty(_currentItem.SavedDefinition))
                    return _currentItem.SavedDefinition;

                return _currentItem.DictionaryWord?.Meanings.FirstOrDefault()?.Definitions.FirstOrDefault()?.DefinitionText
                       ?? "Brak definicji";
            }
        }

        public string CurrentExample
        {
            get
            {
                if (_currentItem == null) return "";

                if (!string.IsNullOrEmpty(_currentItem.SavedExample))
                    return _currentItem.SavedExample;

                return _currentItem.DictionaryWord?.Meanings.FirstOrDefault()?.Definitions.FirstOrDefault()?.Example ?? "";
            }
        }

        public string CurrentPartOfSpeech
        {
            get
            {
                if (_currentItem == null) return "";

                if (!string.IsNullOrEmpty(_currentItem.Context))
                    return _currentItem.Context; // np. "noun"

                return _currentItem.DictionaryWord?.Meanings.FirstOrDefault()?.PartOfSpeech ?? "";
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLearning))]
        private bool _isFinished;

        public bool IsLearning => !IsFinished;

        public FlashcardsViewModel(CollectionService collectionService)
        {
            _collectionService = collectionService;
        }

        async partial void OnCollectionChanged(WordCollection? value)
        {
            if (value != null)
            {
                await StartLearning(value);
            }
        }

        private async Task StartLearning(WordCollection collection)
        {
            var cards = await _collectionService.GetItemsForLearning(collection.Id);

            if (cards.Count == 0)
            {
                IsFinished = true;
                return;
            }

            var shuffled = cards.OrderBy(a => Guid.NewGuid()).ToList();
            _itemsQueue = new Queue<CollectionItem>(shuffled);

            IsFinished = false;
            NextCard();
        }

        [RelayCommand]
        public void NextCard()
        {
            if (_itemsQueue.Count > 0)
            {
                CurrentItem = _itemsQueue.Dequeue();
                IsFinished = false;

                OnPropertyChanged(nameof(CurrentWordText));
                OnPropertyChanged(nameof(CurrentPhonetic));
                OnPropertyChanged(nameof(CurrentTranslation));
                OnPropertyChanged(nameof(CurrentDefinition));
                OnPropertyChanged(nameof(CurrentExample));
                OnPropertyChanged(nameof(CurrentPartOfSpeech));
                OnPropertyChanged(nameof(CurrentImageUrl));
                OnPropertyChanged(nameof(CurrentAudio));
            }
            else
            {
                CurrentItem = null;
                IsFinished = true;
            }
        }

        [RelayCommand]
        public void MarkAsKnown()
        {
            if (CurrentItem != null)
            {
                // postêp w bazie (¿e u¿ytkownik ju¿ umie to s³owo)
                // CurrentItem.IsLearned = true;
                // await _collectionService.UpdateItemAsync(CurrentItem);
            }
            NextCard();
        }

        [RelayCommand]
        public void MarkAsUnknown()
        {
            if (CurrentItem != null)
            {
                //_itemsQueue.Enqueue(CurrentItem);
            }
            NextCard();
        }

        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}