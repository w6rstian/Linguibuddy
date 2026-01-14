using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Linguibuddy.ViewModels;

[QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
public partial class ImageQuizViewModel : BaseQuizViewModel
{
    private readonly ICollectionService _collectionService;
    private readonly IScoringService _scoringService;
    private readonly IAppUserService _appUserService;
    private List<CollectionItem> allWords;
    private Random random = Random.Shared;

    [ObservableProperty] private List<CollectionItem> _hasAppeared;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLearning))]
    private bool _isFinished;

    [ObservableProperty] private int _pointsEarned;

    [ObservableProperty] private int _score;

    [ObservableProperty] private WordCollection? _selectedCollection;

    [ObservableProperty] private CollectionItem? _targetWord;

    public ImageQuizViewModel(ICollectionService collectionService, IScoringService scoringService, IAppUserService appUserService)
    {
        _collectionService = collectionService;
        _scoringService = scoringService;
        _appUserService = appUserService;

        _hasAppeared = [];
        IsFinished = false;
        Score = 0;
        PointsEarned = 0;
    }

    public bool IsLearning => !IsFinished;

    public ObservableCollection<QuizOption> Options { get; } = new();

    public async Task ImportCollectionAsync()
    {
        if (SelectedCollection is null || !SelectedCollection.Items.Any())
            return;

        allWords = SelectedCollection.Items
                .OrderBy(_ => random.Next())
                .Take(await _appUserService.GetUserLessonLengthAsync())
                .ToList();
    }

    public override async Task LoadQuestionAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsAnswered = false;
        FeedbackMessage = string.Empty;
        FeedbackColor =
            Application.Current.RequestedTheme == AppTheme.Light
                ? Application.Current.Resources["PrimaryDarkText"] as Color
                : Colors.White;
        Options.Clear();

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await Shell.Current.DisplayAlert(AppResources.NetworkError, AppResources.NetworkRequired, "OK");
            IsBusy = false;
            return;
        }

        try
        {
            if (SelectedCollection == null || SelectedCollection.Items == null || !SelectedCollection.Items.Any())
            {
                FeedbackMessage = AppResources.CollectionEmpty;
                IsFinished = true;
                return;
            }

            if (allWords.Count < 4)
            {
                FeedbackMessage = AppResources.TooLittleWords;
                return;
            }

            var validWords = allWords.Except(HasAppeared).ToList();

            if (validWords.Count == 0)
            {
                // QUIZ IS FINISHED HERE
                IsFinished = true;
                return;
            }

            TargetWord = validWords[random.Next(validWords.Count)];

            var wrongOptions = allWords
                .Where(w => w != TargetWord)
                .OrderBy(_ => random.Next())
                .Take(3)
                .ToList();

            var optionsList = new List<CollectionItem> { TargetWord };
            optionsList.AddRange(wrongOptions);
            optionsList = optionsList
                .OrderBy(_ => random.Next())
                .ToList();

            foreach (var word in optionsList) Options.Add(new QuizOption(word));

            HasAppeared.Add(TargetWord);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Image Quiz Error: {ex.Message}");
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FailedLoadQuestion, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectAnswer(QuizOption selectedOption)
    {
        if (IsAnswered || selectedOption == null || TargetWord == null) return;

        IsAnswered = true;

        if (selectedOption.Word.Id == TargetWord.Id)
        {
            selectedOption.BackgroundColor = Colors.LightGreen;
            Score++;

            var points = _scoringService.CalculatePoints(GameType.ImageQuiz, DifficultyLevel.A1);
            PointsEarned += points;

            FeedbackMessage = AppResources.CorrectAnswer;
            FeedbackColor = Colors.Green;
        }
        else
        {
            selectedOption.BackgroundColor = Colors.Salmon;
            FeedbackMessage = $"{AppResources.IncorrectAnswer} {TargetWord.Word}";
            FeedbackColor = Colors.Red;

            var correct = Options.FirstOrDefault(o => o.Word.Id == TargetWord.Id);
            if (correct != null) correct.BackgroundColor = Colors.LightGreen;
        }
    }

    [RelayCommand]
    public async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task NextQuestionAsync()
    {
        await LoadQuestionAsync();

        if (IsFinished)
            if (SelectedCollection != null)
                await _scoringService.SaveResultsAsync(
                    SelectedCollection,
                    GameType.ImageQuiz,
                    Score,
                    allWords.Count,
                    PointsEarned
                );
    }
}