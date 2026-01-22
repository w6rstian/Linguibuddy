using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;

namespace Linguibuddy.ViewModels;

public enum LearningMode
{
    Standard,
    SpacedRepetition
}

[QueryProperty(nameof(Collection), "Collection")]
[QueryProperty(nameof(CurrentLearningMode), "Mode")]
public partial class FlashcardsViewModel : ObservableObject
{
    private readonly ICollectionService _collectionService;
    private readonly ISpacedRepetitionService _srsService;

    [ObservableProperty] private WordCollection? _collection;

    [ObservableProperty] private CollectionItem? _currentItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStandardMode))]
    [NotifyPropertyChangedFor(nameof(IsSrsMode))]
    private LearningMode _currentLearningMode;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanShowButtons))]
    private bool _isAnswerRevealed;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLearning))]
    private bool _isFinished;

    private Queue<CollectionItem> _itemsQueue = new();

    public FlashcardsViewModel(ICollectionService collectionService, ISpacedRepetitionService srsService)
    {
        _collectionService = collectionService;
        _srsService = srsService;
    }

    public bool IsStandardMode => CurrentLearningMode == LearningMode.Standard;
    public bool IsSrsMode => CurrentLearningMode == LearningMode.SpacedRepetition;
    public bool CanShowButtons => IsAnswerRevealed;
    public bool IsLearning => !IsFinished;

    public string CurrentWordText => CurrentItem?.Word ?? "";
    public string CurrentPhonetic => CurrentItem?.Phonetic ?? "";
    public string CurrentAudio => CurrentItem?.Audio ?? "";
    public string CurrentImageUrl => CurrentItem?.ImageUrl ?? "";

    public string CurrentTranslation =>
        !string.IsNullOrEmpty(CurrentItem?.SavedTranslation)
            ? CurrentItem.SavedTranslation
            : AppResources.NoTranslation;

    public string CurrentDefinition
    {
        get
        {
            if (CurrentItem == null) return "";

            return CurrentItem?.Definition ?? "";
        }
    }

    public string CurrentExample
    {
        get
        {
            if (CurrentItem == null) return "";

            return CurrentItem?.Example ?? "";
        }
    }

    public string CurrentPartOfSpeech
    {
        get
        {
            if (CurrentItem == null) return "";

            return CurrentItem?.PartOfSpeech ?? "";
        }
    }

    async partial void OnCollectionChanged(WordCollection? value)
    {
        if (value != null) await StartSession();
    }

    protected virtual async Task StartSession()
    {
        if (Collection == null)
        {
            await GoBack();
            return;
        }
        
        List<CollectionItem> items;

        if (IsSrsMode)
        {
            items = await _collectionService.GetItemsDueForLearning(Collection.Id);
            if (items.Count == 0)
            {
                await ShowAlertAsync(AppResources.Error, AppResources.NoFlashcardsToLearn, AppResources.OK);
                await GoBack();
                return;
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
    public async Task GradeNull()
    {
        await ProcessSrsGrade(SuperMemoGrade.Null);
        // 0 (Blackout)
    }

    [RelayCommand]
    public async Task GradeHard()
    {
        await ProcessSrsGrade(SuperMemoGrade.Fail);
        // 2 (Incorrect/Hard)
    }

    [RelayCommand]
    public async Task GradeGood()
    {
        await ProcessSrsGrade(SuperMemoGrade.Good);
        // 4 (Good)
    }

    [RelayCommand]
    public async Task GradeEasy()
    {
        await ProcessSrsGrade(SuperMemoGrade.Bright);
        // 5 (Perfect)
    }

    private async Task ProcessSrsGrade(int grade)
    {
        if (CurrentItem?.FlashcardProgress == null)
        {
            NextCard();
            return;
        }

        _srsService.ProcessResult(CurrentItem.FlashcardProgress, grade);
        await _collectionService.UpdateFlashcardProgress(CurrentItem.FlashcardProgress);

        if (grade < SuperMemoGrade.PassingThreshold) _itemsQueue.Enqueue(CurrentItem);

        NextCard();
    }

    [RelayCommand]
    public void MarkAsKnown()
    {
        if (CurrentItem != null)
        {
            // postęp w bazie (że użytkownik już umie to słowo)
            //CurrentItem.IsLearned = true;
            // await _collectionService.UpdateItemAsync(CurrentItem);
        }

        NextCard();
    }

    [RelayCommand]
    public void MarkAsUnknown()
    {
        if (CurrentItem != null) _itemsQueue.Enqueue(CurrentItem);
        NextCard();
    }

    [RelayCommand]
    public async Task GoBack()
    {
        await GoToAsync("..");
    }

    protected virtual Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }

    protected virtual Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }
}