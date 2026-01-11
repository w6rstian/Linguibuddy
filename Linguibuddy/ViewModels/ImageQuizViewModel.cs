using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using LocalizationResourceManager.Maui;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Linguibuddy.ViewModels
{
    public enum QuizSourceType
    {
        Random,
        Collection
    }

    [QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
    public partial class ImageQuizViewModel : BaseQuizViewModel
    {
        private readonly DictionaryApiService _dictionaryService;
        private readonly CollectionService _collectionService;

        [ObservableProperty]
        private bool _isSetupVisible = true;

        [ObservableProperty]
        private bool _isGameVisible = false;

        [ObservableProperty]
        private QuizSourceType _selectedSourceType = QuizSourceType.Random;

        [ObservableProperty]
        private WordCollection? _selectedCollection;

        public ObservableCollection<WordCollection> AvailableCollections { get; } = new();

        private List<DictionaryWord> _collectionPool = new();

        [ObservableProperty]
        private CollectionItem? _targetWord;

        [ObservableProperty]
        private List<CollectionItem> _hasAppeared;
        [ObservableProperty]
        private bool _isFinished;
        [ObservableProperty]
        private int _score;

        public ObservableCollection<QuizOption> Options { get; } = new();

        public ImageQuizViewModel(DictionaryApiService dictionaryService, CollectionService collectionService)
        {
            _dictionaryService = dictionaryService;
            _collectionService = collectionService;
            _hasAppeared = [];

            LoadCollectionsAsync();
        }

        private async void LoadCollectionsAsync()
        {
            var collections = await _collectionService.GetUserCollectionsAsync();
            AvailableCollections.Clear();
            foreach (var c in collections) AvailableCollections.Add(c);
        }

        public override async Task LoadQuestionAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            IsAnswered = false;
            FeedbackMessage = string.Empty;
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
                    FeedbackMessage = "Collection is empty.";
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
                    Options.Add(new QuizOption(word));
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
        private void SelectAnswer(QuizOption selectedOption)
        {
            if (IsAnswered || selectedOption == null || TargetWord == null) return;

            IsAnswered = true;

            if (selectedOption.Word.Id == TargetWord.Id)
            {
                selectedOption.BackgroundColor = Colors.LightGreen;
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
        private async Task NextQuestionAsync()
        {
            await LoadQuestionAsync();

            if (IsFinished)
            {
                //
            }
        }
    }
}
