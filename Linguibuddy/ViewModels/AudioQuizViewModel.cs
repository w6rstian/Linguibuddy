using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Linguibuddy.ViewModels
{
    public partial class AudioQuizViewModel : BaseQuizViewModel
    {
        private readonly DictionaryApiService _dictionaryService;
        private readonly IAudioManager _audioManager;
        private IAudioPlayer? _audioPlayer;

        [ObservableProperty]
        private DictionaryWord? _targetWord;

        public ObservableCollection<QuizOption> Options { get; } = new();

        public AudioQuizViewModel(DictionaryApiService dictionaryService, IAudioManager audioManager)
        {
            _dictionaryService = dictionaryService;
            _audioManager = audioManager;
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
                // 4 losowe słowa z bazy (te, które mają audio)
                var words = await _dictionaryService.GetRandomWordsForGameAsync(4);

                if (words.Count < 4)
                {
                    FeedbackMessage = "Za mało słów w bazie, by utworzyć quiz.";
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
                Debug.WriteLine($"Error loading quiz: {ex.Message}");
                await Shell.Current.DisplayAlert("Błąd", "Nie udało się załadować pytania", "OK");
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
                FeedbackMessage = "Świetnie! To poprawna odpowiedź.";
                FeedbackColor = Colors.Green;
            }
            else
            {
                selectedOption.BackgroundColor = Colors.Salmon;
                FeedbackMessage = $"Niestety. Prawidłowa odpowiedź to: {TargetWord.Word}";
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
                await Shell.Current.DisplayAlert("Błąd Audio", "Nie udało się odtworzyć dźwięku.", "OK");
                Debug.WriteLine($"Audio Error: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task NextQuestionAsync()
        {
            await LoadQuestionAsync();
        }
    }
}