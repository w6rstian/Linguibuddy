using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Linguibuddy.Helpers;

namespace Linguibuddy.ViewModels
{
    [QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
    public partial class ImageQuizViewModel : BaseQuizViewModel
    {
        private readonly CollectionService _collectionService;
        private readonly ScoringService _scoringService;

        [ObservableProperty]
        private WordCollection? _selectedCollection;

        [ObservableProperty]
        private CollectionItem? _targetWord;

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

        public ObservableCollection<Models.QuizOption> Options { get; } = new();

        public ImageQuizViewModel(CollectionService collectionService, ScoringService scoringService)
        {
            _collectionService = collectionService;
            _scoringService = scoringService;

            _hasAppeared = [];
            IsFinished = false;
            Score = 0;
            PointsEarned = 0;
        }

        public override async Task LoadQuestionAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            IsAnswered = false;
            FeedbackMessage = string.Empty;
            FeedbackColor =
                Application.Current.RequestedTheme == AppTheme.Light ?
                Application.Current.Resources["PrimaryDarkText"] as Color :
                Colors.White;
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

                // TODO: This minigame crashes app without errors/exceptions???

                var allWords = SelectedCollection.Items;

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

                var random = Random.Shared;
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

                foreach (var word in optionsList)
                {
                    Options.Add(new Models.QuizOption(word));
                }

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
        private void SelectAnswer(Models.QuizOption selectedOption)
        {
            if (IsAnswered || selectedOption == null || TargetWord == null) return;

            IsAnswered = true;

            if (selectedOption.Word.Id == TargetWord.Id)
            {
                selectedOption.BackgroundColor = Colors.LightGreen;
                Score++;

                int points = _scoringService.CalculatePoints(GameType.ImageQuiz, DifficultyLevel.A1);
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
            {
                if (SelectedCollection != null)
                {
                    await _scoringService.SaveResultsAsync(
                        SelectedCollection,
                        GameType.ImageQuiz,
                        Score,
                        SelectedCollection.Items.Count,
                        PointsEarned
                    );
                }
            }
        }
    }
}
