using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;

namespace Linguibuddy.ViewModels;

[QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
public partial class HangmanViewModel : BaseQuizViewModel
{
    private const int MaxMistakes = 6;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private readonly Random _random = Random.Shared;
    private readonly IScoringService _scoringService;
    private List<CollectionItem> _allWords;

    [ObservableProperty] private string _currentImage;

    [ObservableProperty] private List<CollectionItem> _hasAppeared;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLearning))]
    private bool _isFinished;

    [ObservableProperty] private string _maskedWord;

    [ObservableProperty] private int _mistakes;

    [ObservableProperty] private int _pointsEarned;

    [ObservableProperty] private int _score;

    private string _secretWord = string.Empty;

    [ObservableProperty] private WordCollection? _selectedCollection;

    public HangmanViewModel(IScoringService scoringService, IAppUserService appUserService,
        ILearningService learningService)
    {
        _scoringService = scoringService;
        _appUserService = appUserService;
        _learningService = learningService;

        _hasAppeared = [];
        PointsEarned = 0;
        Score = 0;
        GenerateKeyboard();
    }

    public bool IsLearning => !IsFinished;

    // Klawiatura A-Z
    public ObservableCollection<HangmanLetter> Keyboard { get; } = [];

    private void GenerateKeyboard()
    {
        Keyboard.Clear();
        for (var c = 'A'; c <= 'Z'; c++) Keyboard.Add(new HangmanLetter(c, Colors.Gray));
    }

    public async Task ImportCollectionAsync()
    {
        if (SelectedCollection is null || !SelectedCollection.Items.Any())
            return;

        _allWords = SelectedCollection.Items
            .GroupBy(i => i.Word, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(_ => _random.Next())
            .Take(await _appUserService.GetUserLessonLengthAsync())
            .ToList();
    }

    public override async Task LoadQuestionAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsAnswered = false;
        Mistakes = 0;
        CurrentImage = "hangman_0.jpg";
        FeedbackMessage = string.Empty;

        var theme = GetApplicationTheme();
        FeedbackColor =
            theme == AppTheme.Light
                ? GetColorResource("PrimaryDarkText") ?? Colors.Black
                : Colors.White;

        var keyColor =
            theme == AppTheme.Light
                ? GetColorResource("Primary") ?? Colors.Gray
                : GetColorResource("PrimaryDark") ?? Colors.Gray;

        // Reset klawiatury
        foreach (var key in Keyboard)
        {
            key.IsEnabled = true;
            key.BackgroundColor = Colors.Transparent;
            key.BorderColor = keyColor;
            key.TextColor = keyColor;
        }

        try
        {
            if (SelectedCollection == null || SelectedCollection.Items == null || !SelectedCollection.Items.Any())
            {
                FeedbackMessage = AppResources.CollectionEmpty;
                IsFinished = true;
                return;
            }

            var validWords = _allWords.Except(HasAppeared).ToList();

            if (!validWords.Any())
            {
                // NO WORDS REMAINING. END GAME
                IsFinished = true;
                return;
            }

            if (_allWords is not null && _allWords.Any())
            {
                var wordObj = validWords[_random.Next(validWords.Count)];

                _secretWord = wordObj.Word.Trim().ToUpper();
                HasAppeared.Add(wordObj);

                UpdateMaskedWord();
            }
            else
            {
                FeedbackMessage = AppResources.NoWordsInDatabase;
                MaskedWord = AppResources.Error;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Hangman error: {ex.Message}");
            await ShowAlertAsync(AppResources.Error, AppResources.FailedWordRetrieval, AppResources.OK);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateMaskedWord(List<char>? guessedLetters = null)
    {
        var sb = new StringBuilder();
        var isWon = true;

        foreach (var c in _secretWord)
            if (!char.IsLetter(c))
            {
                // Znaki specjalne (spacja, myślnik)
                sb.Append($"{c} ");
            }
            else if (guessedLetters != null && guessedLetters.Contains(c))
            {
                // Jeśli odgadnięta - pokaż
                sb.Append($"{c} ");
            }
            else
            {
                // Jeśli nie - pokaż podkreślenie
                sb.Append("_ ");
                isWon = false;
            }

        MaskedWord = sb.ToString().Trim();

        if (isWon && !string.IsNullOrEmpty(_secretWord)) GameOver(true);
    }

    [RelayCommand]
    private void GuessLetter(HangmanLetter letterObj)
    {
        if (IsAnswered || !letterObj.IsEnabled) return;

        letterObj.IsEnabled = false;
        var letter = letterObj.Character;

        if (_secretWord.Contains(letter))
        {
            letterObj.BackgroundColor = Colors.LightGreen;
            letterObj.BorderColor = Colors.Transparent;
            letterObj.TextColor = Colors.White;

            var guessed = Keyboard.Where(k => !k.IsEnabled).Select(k => k.Character).ToList();
            UpdateMaskedWord(guessed);
        }
        else
        {
            letterObj.BackgroundColor = Colors.Salmon;
            letterObj.BorderColor = Colors.Transparent;
            letterObj.TextColor = Colors.White;

            Mistakes++;
            CurrentImage = $"hangman_{Mistakes}.jpg";

            if (Mistakes >= MaxMistakes) GameOver(false);
        }
    }

    private void GameOver(bool won)
    {
        IsAnswered = true;
        if (won)
        {
            Score++;

            var points = _scoringService.CalculatePoints(GameType.Hangman, DifficultyLevel.A1);
            PointsEarned += points;

            FeedbackMessage = AppResources.Victory;
            FeedbackColor = Colors.Green;
        }
        else
        {
            FeedbackMessage = $"{AppResources.Defeat}. {AppResources.TheWordIs}:\n{_secretWord}";
            FeedbackColor = Colors.Red;
            // Odkrywamy całe hasło na koniec
            MaskedWord = string.Join(" ", _secretWord.ToCharArray());
        }
    }

    [RelayCommand]
    public async Task GoBack()
    {
        await GoToAsync("..");
    }

    [RelayCommand]
    private async Task NextGameAsync()
    {
        await LoadQuestionAsync();

        if (IsFinished)
            if (SelectedCollection != null)
            {
                await _learningService.MarkLearnedTodayAsync();
                await _scoringService.SaveResultsAsync(
                    SelectedCollection,
                    GameType.Hangman,
                    Score,
                    _allWords.Count,
                    PointsEarned
                );
            }
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

    protected virtual Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }

    protected virtual Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }
}