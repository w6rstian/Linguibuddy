using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace Linguibuddy.ViewModels
{
    [QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
    public partial class HangmanViewModel : BaseQuizViewModel
    {
        private readonly DictionaryApiService _dictionaryService;
        private const int MaxMistakes = 6;
        private string _secretWord = string.Empty;

        // Wyświetlane hasło (np. "A _ P _ E")
        [ObservableProperty]
        private string _maskedWord;

        // Licznik błędów
        [ObservableProperty]
        private int _mistakes;

        // Nazwa pliku obrazka (np. "hangman_0.png")
        [ObservableProperty]
        private string _currentImage;

        [ObservableProperty]
        private WordCollection? _selectedCollection;
        [ObservableProperty]
        private List<CollectionItem> _hasAppeared;
        [ObservableProperty]
        private bool _isFinished;
        [ObservableProperty]
        private int _score;

        // Klawiatura A-Z
        public ObservableCollection<HangmanLetter> Keyboard { get; } = new();

        public HangmanViewModel(DictionaryApiService dictionaryService)
        {
            _dictionaryService = dictionaryService;
            _hasAppeared = [];
            Title = "Hangman";
            // Inicjalizacja klawiatury pustymi wartościami, zostanie odświeżona przy LoadQuestion
            GenerateKeyboard();
        }

        private void GenerateKeyboard()
        {
            Keyboard.Clear();
            // Kolor domyślny nie ma tu znaczenia, ustawimy go w LoadQuestionAsync
            for (char c = 'A'; c <= 'Z'; c++)
            {
                Keyboard.Add(new HangmanLetter(c, Colors.Gray));
            }
        }

        public override async Task LoadQuestionAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            IsAnswered = false;
            Mistakes = 0;
            CurrentImage = "hangman_0.jpg"; // Upewnij się, że masz ten plik w Resources/Images
            FeedbackMessage = "";
            FeedbackColor = Colors.Transparent;

            // Pobranie koloru Primary z zasobów aplikacji (bezpieczny sposób)
            Color primaryColor = Colors.Purple;
            if (Application.Current.Resources.TryGetValue("Primary", out var colorRes))
            {
                primaryColor = (Color)colorRes;
            }

            // Reset klawiatury
            foreach (var key in Keyboard)
            {
                key.IsEnabled = true;
                key.BackgroundColor = Colors.Transparent; // Styl Outline
                key.BorderColor = primaryColor;
                key.TextColor = primaryColor;
            }

            try
            {
                if (SelectedCollection == null || SelectedCollection.Items == null || !SelectedCollection.Items.Any())
                {
                    FeedbackMessage = "Collection is empty.";
                    IsFinished = true;
                    return;
                }

                // Pobieramy 1 losowe słowo z kolekcji
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
                    // Normalizujemy słowo (tylko litery, uppercase)
                    _secretWord = wordObj.Word.Trim().ToUpper();
                    HasAppeared.Add(wordObj);

                    UpdateMaskedWord();
                }
                else
                {
                    FeedbackMessage = AppResources.NoWordsInDatabase;
                    MaskedWord = "ERROR";
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
                    // Znaki specjalne (spacja, myślnik) pokazujemy od razu
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
        private void GuessLetter(HangmanLetter letterObj)
        {
            if (IsAnswered || !letterObj.IsEnabled) return;

            letterObj.IsEnabled = false; // Blokujemy przycisk
            char letter = letterObj.Character;

            if (_secretWord.Contains(letter))
            {
                // TRAFIONY (Zielony)
                letterObj.BackgroundColor = Colors.LightGreen; // Lub pobierz success color
                letterObj.BorderColor = Colors.Transparent;
                letterObj.TextColor = Colors.White;

                var guessed = Keyboard.Where(k => !k.IsEnabled).Select(k => k.Character).ToList();
                UpdateMaskedWord(guessed);
            }
            else
            {
                // PUDŁO (Czerwony/Salmon)
                letterObj.BackgroundColor = Colors.Salmon;
                letterObj.BorderColor = Colors.Transparent;
                letterObj.TextColor = Colors.White;

                Mistakes++;
                CurrentImage = $"hangman_{Mistakes}.jpg"; // Zmiana obrazka

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
                FeedbackMessage = AppResources.Victory;
                FeedbackColor = Colors.Green;
                Score++;
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
        private async Task NextGameAsync()
        {
            await LoadQuestionAsync();

            if (IsFinished)
            {
                var ScoreText = $"{Score}/{SelectedCollection!.Items.Count}";
                // TODO: Post-game summary
            }
        }
    }
}