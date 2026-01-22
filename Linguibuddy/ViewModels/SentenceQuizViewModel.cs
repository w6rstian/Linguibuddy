using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;

namespace Linguibuddy.ViewModels;

[QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
public partial class SentenceQuizViewModel : BaseQuizViewModel
{
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private readonly IOpenAiService _openAiService;
    private readonly Random _random = Random.Shared;
    private readonly IScoringService _scoringService;
    private List<CollectionItem> _allWords;

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

        _allWords = SelectedCollection.Items
            .OrderBy(_ => _random.Next())
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

        var theme = GetApplicationTheme();
        FeedbackColor =
            theme == AppTheme.Light
                ? GetColorResource("PrimaryDarkText") ?? Colors.Black
                : Colors.White;

        AvailableWords.Clear();
        SelectedWords.Clear();
        PolishTranslation = AppResources.SentenceGenerating;

        if (!IsNetworkConnected())
        {
            await ShowAlertAsync(AppResources.NetworkError, AppResources.NetworkRequired, AppResources.OK);
            IsBusy = false;
            await GoBack();
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

            TargetWord = validWords[_random.Next(validWords.Count)];

            var difficultyString = _currentDifficulty.ToString();

            var generatedData =
                await _openAiService.GenerateSentenceAsync(TargetWord.Word, difficultyString, TargetWord.Definition);

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

            words = words.OrderBy(x => _random.Next()).ToList();

            foreach (var w in words) AvailableWords.Add(new WordTile(w));

            HasAppeared.Add(TargetWord);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Error loading sentence quiz: {e.Message}");
            await ShowAlertAsync(AppResources.Error, AppResources.FailedLoadQuestion, AppResources.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectWord(WordTile tile)
    {
        if (IsAnswered) return;

        AvailableWords.Remove(tile);
        SelectedWords.Add(tile);
    }

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
            await SpeakAsync(_currentQuestion.EnglishSentence);
        }
        catch (Exception ex)
        {
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
        await GoToAsync("..");
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
                    _allWords.Count,
                    PointsEarned
                );
            }
    }

    protected virtual bool IsNetworkConnected()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }

    protected virtual Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }

    protected virtual Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }

    protected virtual async Task SpeakAsync(string text)
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        string[] femaleVoices = { "Zira", "Paulina", "Jenny", "Aria" };

        var preferred = locales.FirstOrDefault(l =>
                            (l.Language == "en-US" || (l.Language == "en" && l.Country == "US")) &&
                            femaleVoices.Any(f => l.Name.Contains(f)))
                        ?? locales.FirstOrDefault(l =>
                            (l.Language == "en-GB" || (l.Language == "en" && l.Country == "GB")) &&
                            femaleVoices.Any(f => l.Name.Contains(f)))
                        ?? locales.FirstOrDefault(l =>
                            l.Language.StartsWith("en") && femaleVoices.Any(f => l.Name.Contains(f)))
                        ?? locales.FirstOrDefault(l =>
                            l.Language == "en-US" || (l.Language == "en" && l.Country == "US"))
                        ?? locales.FirstOrDefault(l =>
                            l.Language == "en-GB" || (l.Language == "en" && l.Country == "GB"))
                        ?? locales.FirstOrDefault(l => l.Language.StartsWith("en"));

        if (preferred == null)
        {
            await ShowAlertAsync(AppResources.Error, AppResources.InstallEng, AppResources.OK);
            return;
        }

        await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
        {
            Locale = preferred,
            Pitch = 1.0f,
            Volume = 1.0f
        });
    }

    protected virtual AppTheme GetApplicationTheme()
    {
        Debug.Assert(Application.Current != null);
        return Application.Current.RequestedTheme;
    }

    protected virtual Color? GetColorResource(string key)
    {
        Debug.Assert(Application.Current != null);
        return Application.Current.Resources[key] as Color;
    }
}