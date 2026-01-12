using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;

namespace Linguibuddy.ViewModels
{
    public enum LearningMode
    {
        Standard,
        SpacedRepetition
    }

    [QueryProperty(nameof(Collection), "Collection")]
    [QueryProperty(nameof(CurrentLearningMode), "Mode")]
    public partial class FlashcardsViewModel : ObservableObject
    {
        private readonly CollectionService _collectionService;
        private readonly SpacedRepetitionService _srsService;

        private Queue<CollectionItem> _itemsQueue = new();

        [ObservableProperty]
        private WordCollection? _collection;

        [ObservableProperty]
        private CollectionItem? _currentItem;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsStandardMode))]
        [NotifyPropertyChangedFor(nameof(IsSrsMode))]
        private LearningMode _currentLearningMode;
        public bool IsStandardMode => CurrentLearningMode == LearningMode.Standard;
        public bool IsSrsMode => CurrentLearningMode == LearningMode.SpacedRepetition;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanShowButtons))]
        private bool _isAnswerRevealed;
        public bool CanShowButtons => IsAnswerRevealed;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLearning))]
        private bool _isFinished;
        public bool IsLearning => !IsFinished;

        public string CurrentWordText => _currentItem?.Word ?? "";
        public string CurrentPhonetic => _currentItem?.Phonetic ?? "";
        public string CurrentAudio => _currentItem?.Audio ?? "";
        public string CurrentImageUrl => _currentItem?.ImageUrl ?? "";

        public string CurrentTranslation =>
            !string.IsNullOrEmpty(_currentItem?.SavedTranslation)
                ? _currentItem.SavedTranslation
                : "Brak t³umaczenia";

        public string CurrentDefinition
        {
            get
            {
                if (_currentItem == null) return "";

                return _currentItem?.Definition ?? "";
            }
        }

        public string CurrentExample
        {
            get
            {
                if (_currentItem == null) return "";

                return _currentItem?.Example ?? "";
            }
        }

        public string CurrentPartOfSpeech
        {
            get
            {
                if (_currentItem == null) return "";

                return _currentItem?.PartOfSpeech ?? "";
            }
        }

        public FlashcardsViewModel(CollectionService collectionService, SpacedRepetitionService srsService)
        {
            _collectionService = collectionService;
            _srsService = srsService;
        }

        async partial void OnCollectionChanged(WordCollection? value)
        {
            if (value != null)
            {
                await StartSession();
            }
        }

        private async Task StartSession()
        {
            if (Collection == null) await GoBack();
            List<CollectionItem> items;

            if (IsSrsMode)
            {
                items = await _collectionService.GetItemsDueForLearning(Collection.Id);
                if (items.Count == 0)
                {
                    await Shell.Current.DisplayAlert(AppResources.Error, AppResources.NoFlashcardsToLearn, "OK");
                    await GoBack();
                }
            }
            else
            {
                items = await _collectionService.GetItemsForLearning(Collection.Id);
                var rng = new Random();
                items = items.OrderBy(x => rng.Next()).ToList();
            }

            _itemsQueue = new Queue<CollectionItem>(items);
            IsFinished = false;
            NextCard();
        }

        [RelayCommand]
        public void RevealAnswer()
        {
            IsAnswerRevealed = true;
            OnPropertyChanged(nameof(CanShowButtons));
        }

        [RelayCommand]
        public void NextCard()
        {
            IsAnswerRevealed = false;
            OnPropertyChanged(nameof(CanShowButtons));

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
        public async Task GradeNull() => await ProcessSrsGrade(SuperMemoGrade.Null); // 0 (Blackout)

        [RelayCommand]
        public async Task GradeHard() => await ProcessSrsGrade(SuperMemoGrade.Fail); // 2 (Incorrect/Hard)

        [RelayCommand]
        public async Task GradeGood() => await ProcessSrsGrade(SuperMemoGrade.Good); // 4 (Good)

        [RelayCommand]
        public async Task GradeEasy() => await ProcessSrsGrade(SuperMemoGrade.Bright); // 5 (Perfect)

        private async Task ProcessSrsGrade(int grade)
        {
            if (CurrentItem?.FlashcardProgress == null)
            {
                NextCard();
                return;
            }

            _srsService.ProcessResult(CurrentItem.FlashcardProgress, grade);
            await _collectionService.UpdateFlashcardProgress(CurrentItem.FlashcardProgress);

            if (grade < SuperMemoGrade.PassingThreshold)
            {
                _itemsQueue.Enqueue(CurrentItem);
            }

            NextCard();
        }

        [RelayCommand]
        public void MarkAsKnown()
        {
            if (CurrentItem != null)
            {
                // postêp w bazie (¿e u¿ytkownik ju¿ umie to s³owo)
                //CurrentItem.IsLearned = true;
                // await _collectionService.UpdateItemAsync(CurrentItem);
            }
            NextCard();
        }

        [RelayCommand]
        public void MarkAsUnknown()
        {
            if (CurrentItem != null)
            {
                _itemsQueue.Enqueue(CurrentItem);
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