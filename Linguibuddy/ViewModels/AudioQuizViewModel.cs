using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using LocalizationResourceManager.Maui;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace Linguibuddy.ViewModels
{
    [QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
    public partial class AudioQuizViewModel : BaseQuizViewModel
    {
        private readonly DictionaryApiService _dictionaryService;
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _audioPlayer;

        [ObservableProperty]
        private CollectionItem? _targetWord;

        [ObservableProperty]
        private WordCollection? _selectedCollection;

        [ObservableProperty]
        private List<CollectionItem> _hasAppeared;
        [ObservableProperty]
        private bool _isFinished;
        [ObservableProperty]
        private int _score;

        public ObservableCollection<QuizOption> Options { get; } = new();

        public AudioQuizViewModel(DictionaryApiService dictionaryService, IAudioManager audioManager)
        {
            _dictionaryService = dictionaryService;
            _audioManager = audioManager;
            HasAppeared = [];
            IsFinished = false;
            Score = 0;
        }

        public override async Task LoadQuestionAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            IsAnswered = false;
            FeedbackMessage = string.Empty;
            Options.Clear();

            try
            {
                // We only choose allWords that have not been asked as the next word. All allWords can appear as an incorrect QuizOption.
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

                // TODO: CHECK IF TARGET WORD HAS AUDIO AND PHONETIC SPELLING. HANDLE IF NOT

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
                Debug.WriteLine($"Error loading quiz: {ex.Message}");
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FailedLoadQuestion, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SelectAnswerAsync(QuizOption selectedOption)
        {
            if (IsAnswered || selectedOption == null || TargetWord == null) return;

            IsAnswered = true;

            if (selectedOption.Word.Id == TargetWord.Id)
            {
                selectedOption.BackgroundColor = Colors.LightGreen;
                FeedbackMessage = AppResources.CorrectAnswer;
                FeedbackColor = Colors.Green;
                Score++;
            }
            else
            {
                selectedOption.BackgroundColor = Colors.Salmon;
                FeedbackMessage = $"{AppResources.IncorrectAnswer} {TargetWord.Word}";
                FeedbackColor = Colors.Red;

                var correctOption = Options.FirstOrDefault(o => o.Word.Id == TargetWord.Id);
                if (correctOption != null)
                {
                    correctOption.BackgroundColor = Colors.LightGreen;
                }
            }
        }

        [RelayCommand]
        private async Task PlayAudioAsync()
        {
            if (TargetWord == null || string.IsNullOrWhiteSpace(TargetWord.Audio)) return;

            string url = TargetWord.Audio;

            if (_audioPlayer != null && _audioPlayer.IsPlaying)
            {
                _audioPlayer.Dispose();
            }

            try
            {
                using var client = new HttpClient();
                var audioBytes = await client.GetByteArrayAsync(url);

                string fileName = "quiz_temp_audio.mp3";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await File.WriteAllBytesAsync(filePath, audioBytes);

                var fileStream = File.OpenRead(filePath);

                _audioPlayer = _audioManager.CreatePlayer(fileStream);
                _audioPlayer.Play();

                _audioPlayer.PlaybackEnded += (s, e) =>
                {
                    fileStream.Dispose();
                };
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(AppResources.AudioError, AppResources.PlaybackError, "OK");
                Debug.WriteLine($"Audio Error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task NextQuestionAsync()
        {
            await LoadQuestionAsync();

            if (IsFinished)
            {
                // TODO: DisplayResultScreen()
                // Nie rozumiem do końca jak jest zrobiony ekran końcowy w fiszkach, ale tutaj powinno być podobnie.
                // Wyżej przy prawidłowej odpowiedzi jest robiony Score++.
                // Można wyświetlić score/allwords.count, np. "5/10 poprawnych odpowiedzi"

                // A ten score się przyda może do punktów do grywalizacji albo można dodać jeszcze jakieś kolekcje/pola do CollectionItem,
                // żeby śledzić jakie słowa użytkownik umie/nie umie (analiza wyników dla AI).

                // Myślałem, żeby każde słowo w danej kolekcji miało win ratio (czyli potrzebne 2 pola, poprawneOdpCount i wszystkieOdpCount).
                // Oraz dodatkowo pole "ostatnia odp" czy poprawna czy nie. Może to wystarczy, żeby AI zdecydowało czy dane słowo już jest nauczone.
            }
        }

        private async Task DisplayResultScreen()
        {

        }
    }
}