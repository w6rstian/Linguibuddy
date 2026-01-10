using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public partial class SentenceQuizViewModel : BaseQuizViewModel
    {
        public ObservableCollection<WordTile> AvailableWords { get; } = new();

        public ObservableCollection<WordTile> SelectedWords { get; } = new();

        public bool IsNotAnswered => !IsAnswered;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotAnswered))]
        private bool _isAnswered;

        [ObservableProperty]
        private string _polishTranslation;

        private SentenceQuestion? _currentQuestion;

        public SentenceQuizViewModel()
        {
            Title = "Sentence Builder";
        }

        public override async Task LoadQuestionAsync()
        {
            IsBusy = true;
            IsAnswered = false;
            FeedbackMessage = string.Empty;
            FeedbackColor = Colors.Transparent;

            AvailableWords.Clear();
            SelectedWords.Clear();

            // --- MOCK DANYCH (W przyszłości z serwisu/AI) ---
            var mockSentences = new List<SentenceQuestion>
            {
                new() { EnglishSentence = "The cat creates a plan", PolishTranslation = "Kot tworzy plan" },
                new() { EnglishSentence = "I would like to order a coffee", PolishTranslation = "Chciałbym zamówić kawę" },
                new() { EnglishSentence = "Learning programming is fun and useful", PolishTranslation = "Nauka programowania jest fajna i przydatna" },
                new() { EnglishSentence = "Where is the nearest bus stop", PolishTranslation = "Gdzie jest najbliższy przystanek autobusowy" }
            };

            var random = new Random();
            _currentQuestion = mockSentences[random.Next(mockSentences.Count)];

            PolishTranslation = _currentQuestion.PolishTranslation;

            var words = _currentQuestion.EnglishSentence.Split(' ').ToList();

            // Prosty shuffle
            words = words.OrderBy(x => random.Next()).ToList();

            foreach (var word in words)
            {
                AvailableWords.Add(new WordTile(word));
            }

            IsBusy = false;
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

        // Akcja: Sprawdź odpowiedź (przycisk na dole)
        [RelayCommand]
        private void CheckAnswer()
        {
            if (_currentQuestion == null) return;

            // Złóż zdanie z wybranych kafelków
            string formedSentence = string.Join(" ", SelectedWords.Select(w => w.Text));

            bool isCorrect = formedSentence.Trim() == _currentQuestion.EnglishSentence.Trim();

            IsAnswered = true;

            if (isCorrect)
            {
                FeedbackMessage = AppResources.Perfect;
                FeedbackColor = Colors.Green;
            }
            else
            {
                FeedbackMessage = $"{AppResources.ErrorCorrect} {_currentQuestion.EnglishSentence}";
                FeedbackColor = Colors.Red;
            }
        }

        [RelayCommand]
        private async Task NextQuestionAsync()
        {
            await LoadQuestionAsync();
        }
    }
}
