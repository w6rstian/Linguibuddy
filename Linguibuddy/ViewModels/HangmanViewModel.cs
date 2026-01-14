using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
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
using System.Text;

namespace Linguibuddy.ViewModels;

[QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
public partial class HangmanViewModel : BaseQuizViewModel
{
    private const int MaxMistakes = 6;
    private readonly IScoringService _scoringService;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private List<CollectionItem> allWords;
    private Random random = Random.Shared;

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

    public HangmanViewModel(IScoringService scoringService, IAppUserService appUserService, ILearningService learningService)
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
        Mistakes = 0;
        CurrentImage = "hangman_0.jpg";
        FeedbackMessage = string.Empty;
        FeedbackColor =
            Application.Current.RequestedTheme == AppTheme.Light
                ? Application.Current.Resources["PrimaryDarkText"] as Color
                : Colors.White;

        var keyColor =
            Application.Current.RequestedTheme == AppTheme.Light
                ? Application.Current.Resources["Primary"] as Color
                : Application.Current.Resources["PrimaryDark"] as Color;

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

            var validWords = allWords.Except(HasAppeared).ToList();

            if (!validWords.Any())
            {
                // NO WORDS REMAINING. END GAME
                IsFinished = true;
                return;
            }

            if (allWords is not null && allWords.Any())
            {
                var wordObj = validWords[random.Next(validWords.Count)];

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
            await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FailedWordRetrieval, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateMaskedWord(List<char> guessedLetters = null)
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
                // Jeśli odgadnięta -> pokaż
                sb.Append($"{c} ");
            }
            else
            {
                // Jeśli nie -> pokaż podkreślenie
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
        await Shell.Current.GoToAsync("..");
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
                    allWords.Count,
                    PointsEarned
                );
            }
    }
}