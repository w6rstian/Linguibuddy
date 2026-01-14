using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Linguibuddy.Interfaces;

namespace Linguibuddy.ViewModels;

[QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
public partial class SentenceQuizViewModel : BaseQuizViewModel
{
    private readonly IAppUserService _appUserService;
    private readonly IOpenAiService _openAiService;
    private readonly IScoringService _scoringService;
    private readonly ILearningService _learningService;
    private List<CollectionItem> allWords;
    private Random random = Random.Shared;

    private DifficultyLevel _currentDifficulty;

    private SentenceQuestion? _currentQuestion;

    [ObservableProperty] private List<CollectionItem> _hasAppeared;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsNotAnswered))]
    private bool _isAnswered;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLearning))]
    private bool _isFinished;

    [ObservableProperty] private int _pointsEarned;

    [ObservableProperty] private string _polishTranslation;

    [ObservableProperty] private int _score;

    [ObservableProperty] private WordCollection? _selectedCollection;

    [ObservableProperty] private CollectionItem? _targetWord;

    public SentenceQuizViewModel(IOpenAiService openAiService, IScoringService scoringService,
        IAppUserService appUserService, ILearningService learningService)
    {
        _openAiService = openAiService;
        _scoringService = scoringService;
        _appUserService = appUserService;
        _learningService = learningService;

        HasAppeared = [];
        IsFinished = false;
        Score = 0;
        PointsEarned = 0;
    }

    public bool IsLearning => !IsFinished;

    public ObservableCollection<WordTile> AvailableWords { get; } = [];
    public ObservableCollection<WordTile> SelectedWords { get; } = [];
    public bool IsNotAnswered => !IsAnswered;

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
        _currentDifficulty = await _appUserService.GetUserDifficultyAsync();

        if (IsBusy) return;

        IsBusy = true;
        IsAnswered = false;
        FeedbackMessage = string.Empty;
        FeedbackColor =
            Application.Current.RequestedTheme == AppTheme.Light
                ? Application.Current.Resources["PrimaryDarkText"] as Color
                : Colors.White;

        AvailableWords.Clear();
        SelectedWords.Clear();
        PolishTranslation = AppResources.SentenceGenerating;

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

            var validWords = SelectedCollection.Items.Except(HasAppeared).ToList();

            if (validWords.Count == 0)
            {
                IsFinished = true;
                PolishTranslation = string.Empty;
                return;
            }

            TargetWord = validWords[random.Next(validWords.Count)];

            //int difficulty = Preferences.Default.Get(Constants.DifficultyLevelKey, (int)DifficultyLevel.A1);
            //var difficulty = await _appUserService.GetUserDifficultyAsync();
            var difficultyString = _currentDifficulty.ToString();

            var generatedData = await _openAiService.GenerateSentenceAsync(TargetWord.Word, difficultyString);

            if (generatedData != null)
                _currentQuestion = new SentenceQuestion
                {
                    EnglishSentence = generatedData.Value.English,
                    PolishTranslation = generatedData.Value.Polish
                };
            else
                _currentQuestion = new SentenceQuestion
                {
                    EnglishSentence = $"I am learning the word {TargetWord.Word}",
                    PolishTranslation = $"Uczę się słowa {TargetWord.Word} (Error Mode)"
                };

            PolishTranslation = _currentQuestion.PolishTranslation;

            var cleanSentence = _currentQuestion.EnglishSentence
                .Replace(".", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace(",", "");

            var words = cleanSentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            words = words.OrderBy(x => random.Next()).ToList();

            foreach (var w in words) AvailableWords.Add(new WordTile(w));

            HasAppeared.Add(TargetWord);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Error loading sentence quiz: {e.Message}");
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FailedLoadQuestion, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Akcja: Kliknięcie w słowo na dole (dodaj do zdania)
    [RelayCommand]
    private void SelectWord(WordTile tile)
    {
        if (IsAnswered) return;

        AvailableWords.Remove(tile);
        SelectedWords.Add(tile);
    }

    // Akcja: Kliknięcie w słowo u góry (usuń ze zdania)
    [RelayCommand]
    private void DeselectWord(WordTile tile)
    {
        if (IsAnswered) return;

        SelectedWords.Remove(tile);
        AvailableWords.Add(tile);
    }

    [RelayCommand]
    public async Task ReadSentence()
    {
        if (_currentQuestion == null) return;

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            string[] femaleVoices = { "Zira", "Paulina", "Jenny", "Aria" };
            // preferowany język US i GB na Android i Windows
            var preferred = locales.FirstOrDefault(l =>
                                (l.Language == "en-US" || (l.Language == "en" && l.Country == "US")) &&
                                femaleVoices.Any(f => l.Name.Contains(f)))
                            ?? locales.FirstOrDefault(l =>
                                (l.Language == "en-GB" || (l.Language == "en" && l.Country == "GB")) &&
                                femaleVoices.Any(f => l.Name.Contains(f)))
                            ?? locales.FirstOrDefault(l =>
                                l.Language.StartsWith("en") && femaleVoices.Any(f => l.Name.Contains(f)))
                            // inne głosy
                            ?? locales.FirstOrDefault(l =>
                                l.Language == "en-US" || (l.Language == "en" && l.Country == "US"))
                            ?? locales.FirstOrDefault(l =>
                                l.Language == "en-GB" || (l.Language == "en" && l.Country == "GB"))
                            ?? locales.FirstOrDefault(l => l.Language.StartsWith("en"));

            if (preferred == null)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.InstallEng, "OK");
                return;
            }

            await TextToSpeech.Default.SpeakAsync(_currentQuestion.EnglishSentence, new SpeechOptions
            {
                Locale = preferred,
                Pitch = 1.0f,
                Volume = 1.0f
            });
        }
        catch (Exception ex)
        {
            //await Shell.Current.DisplayAlert(AppResources.AudioError, AppResources.PlaybackError, "OK");
            Debug.WriteLine(ex.Message);
        }
    }

    [RelayCommand]
    private void CheckAnswer()
    {
        if (_currentQuestion == null) return;

        var formedSentence = string.Join(" ", SelectedWords.Select(w => w.Text));

        var correctSentence = _currentQuestion.EnglishSentence;

        string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var lower = input.ToLowerInvariant();

            var punctuation = new[] { '.', ',', '?', '!', ';', ':', '-', '"', '\'' };
            foreach (var p in punctuation) lower = lower.Replace(p.ToString(), "");
            return string.Join(" ", lower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        var isCorrect = Normalize(formedSentence) == Normalize(correctSentence);

        IsAnswered = true;

        if (isCorrect)
        {
            Score++;

            //int difficultyInt = Preferences.Default.Get(Constants.DifficultyLevelKey, (int)DifficultyLevel.A1);
            //var difficulty = (DifficultyLevel)difficultyInt;

            var points = _scoringService.CalculatePoints(GameType.SentenceQuiz, _currentDifficulty);
            PointsEarned += points;

            FeedbackMessage = AppResources.Perfect;
            FeedbackColor = Colors.Green;
        }
        else
        {
            FeedbackMessage = $"{AppResources.ErrorCorrect}\n{_currentQuestion.EnglishSentence}";
            FeedbackColor = Colors.Red;
        }

        _ = ReadSentence();
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
            {
                await _learningService.MarkLearnedTodayAsync();
                await _scoringService.SaveResultsAsync(
                    SelectedCollection,
                    GameType.SentenceQuiz,
                    Score,
                    allWords.Count,
                    PointsEarned
                );
            }
    }
}