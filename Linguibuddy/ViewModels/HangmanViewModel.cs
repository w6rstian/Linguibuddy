using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Text;
using Linguibuddy.Helpers;

namespace Linguibuddy.ViewModels
{
    [QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
    public partial class HangmanViewModel : BaseQuizViewModel
    {
        private readonly ScoringService _scoringService;

        private const int MaxMistakes = 6;
        private string _secretWord = string.Empty;

        [ObservableProperty]
        private string _maskedWord;

        [ObservableProperty]
        private int _mistakes;

        [ObservableProperty]
        private string _currentImage;

        [ObservableProperty]
        private WordCollection? _selectedCollection;
        [ObservableProperty]
        private List<CollectionItem> _hasAppeared;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLearning))]
        private bool _isFinished;
        public bool IsLearning => !IsFinished;

        [ObservableProperty]
        private int _score;

        [ObservableProperty]
        private int _pointsEarned;

        // Klawiatura A-Z
        public ObservableCollection<Models.HangmanLetter> Keyboard { get; } = [];

        public HangmanViewModel(ScoringService scoringService)
        {
            _scoringService = scoringService;

            _hasAppeared = [];
            PointsEarned = 0;
            Score = 0;
            GenerateKeyboard();
        }

        private void GenerateKeyboard()
        {
            Keyboard.Clear();
            for (char c = 'A'; c <= 'Z'; c++)
            {
                Keyboard.Add(new Models.HangmanLetter(c, Colors.Gray));
            }
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
                Application.Current.RequestedTheme == AppTheme.Light ?
                Application.Current.Resources["PrimaryDarkText"] as Color :
                Colors.White;

            Color keyColor =
                Application.Current.RequestedTheme == AppTheme.Light ?
                Application.Current.Resources["Primary"] as Color:
                Application.Current.Resources["PrimaryDark"] as Color;

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

                var allWords = SelectedCollection.Items;
                var validWords = allWords.Except(HasAppeared).ToList();

                if (!validWords.Any())
                {
                    // NO WORDS REMAINING. END GAME
                    IsFinished = true;
                    return;
                }

                if (allWords is not null && allWords.Any())
                {
                    var random = Random.Shared;
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
            bool isWon = true;

            foreach (char c in _secretWord)
            {
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
            }

            MaskedWord = sb.ToString().Trim();

            if (isWon && !string.IsNullOrEmpty(_secretWord))
            {
                GameOver(true);
            }
        }

        [RelayCommand]
        private void GuessLetter(Models.HangmanLetter letterObj)
        {
            if (IsAnswered || !letterObj.IsEnabled) return;

            letterObj.IsEnabled = false;
            char letter = letterObj.Character;

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

                if (Mistakes >= MaxMistakes)
                {
                    GameOver(false);
                }
            }
        }

        private void GameOver(bool won)
        {
            IsAnswered = true;
            if (won)
            {
                Score++;

                int points = _scoringService.CalculatePoints(GameType.Hangman, DifficultyLevel.A1);
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
            {
                if (SelectedCollection != null)
                {
                    await _scoringService.SaveResultsAsync(
                        SelectedCollection,
                        GameType.Hangman,
                        Score,
                        SelectedCollection.Items.Count,
                        PointsEarned
                    );
                }
            }
        }
    }
}