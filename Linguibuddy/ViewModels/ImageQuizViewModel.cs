using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Linguibuddy.ViewModels
{
    public enum QuizSourceType
    {
        Random,
        Collection
    }

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
        private DictionaryWord? _targetWord;

        public ObservableCollection<QuizOption> Options { get; } = new();

        public ImageQuizViewModel(DictionaryApiService dictionaryService, CollectionService collectionService)
        {
            _dictionaryService = dictionaryService;
            _collectionService = collectionService;

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
                await Shell.Current.DisplayAlert("Brak sieci", "Ten quiz wymaga połączenia z internetem.", "OK");
                IsBusy = false;
                return;
            }

            try
            {
                var words = await _dictionaryService.GetRandomWordsWithImagesAsync(4);

                if (words.Count < 4)
                {
                    FeedbackMessage = "Za mało słów ze zdjęciami w bazie.";
                    return;
                }

                var random = new Random();
                TargetWord = words[random.Next(words.Count)];

                foreach (var word in words)
                {
                    Options.Add(new QuizOption(word));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Image Quiz Error: {ex.Message}");
                await Shell.Current.DisplayAlert("Błąd", "Nie udało się załadować pytania", "OK");
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
                FeedbackMessage = "Świetnie! Dobra odpowiedź.";
                FeedbackColor = Colors.Green;
            }
            else
            {
                selectedOption.BackgroundColor = Colors.Salmon;
                FeedbackMessage = $"Niestety... To jest: {TargetWord.Word}";
                FeedbackColor = Colors.Red;

                var correct = Options.FirstOrDefault(o => o.Word.Id == TargetWord.Id);
                if (correct != null) correct.BackgroundColor = Colors.LightGreen;
            }
        }

        [RelayCommand]
        private async Task NextQuestionAsync()
        {
            await LoadQuestionAsync();
        }
    }
}
