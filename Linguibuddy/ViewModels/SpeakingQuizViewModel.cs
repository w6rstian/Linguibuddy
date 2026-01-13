using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Models;
using Linguibuddy.Services;
using System.Diagnostics;
using System.Globalization;

namespace Linguibuddy.ViewModels
{
    // TODO: add translations and better view
    [QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
    public partial class SpeakingQuizViewModel : BaseQuizViewModel
    {
        private readonly ISpeechToText _speechToText;
        private readonly OpenAiService _openAiService;

        [ObservableProperty] private WordCollection? _selectedCollection;
        [ObservableProperty] private CollectionItem? _targetWord;
        [ObservableProperty] private List<CollectionItem> _hasAppeared;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLearning))]
        private bool _isFinished;
        public bool IsLearning => !IsFinished;

        [ObservableProperty] private int _score;
        [ObservableProperty] private string _recognizedText;
        [ObservableProperty] private string _targetSentence;
        [ObservableProperty] private string _polishTranslation;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotListening))]
        private bool _isListening;
        public bool IsNotListening => !IsListening;

        public SpeakingQuizViewModel(ISpeechToText speechToText, OpenAiService openAiService)
        {
            _speechToText = speechToText;
            _openAiService = openAiService;

            HasAppeared = [];
            IsFinished = false;
            Score = 0;
            RecognizedText = "Naciśnij 'Słuchaj' i czytaj...";
        }

        public override async Task LoadQuestionAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            IsAnswered = false;

            await ForceStopListening();

            FeedbackMessage = string.Empty;
            FeedbackColor = Colors.Transparent;
            PolishTranslation = "Generowanie zdania...";
            TargetSentence = string.Empty;
            RecognizedText = string.Empty;

            try
            {
                if (SelectedCollection == null || SelectedCollection.Items == null || !SelectedCollection.Items.Any())
                {
                    IsFinished = true;
                    return;
                }

                var validWords = SelectedCollection.Items.Except(HasAppeared).ToList();

                if (validWords.Count == 0)
                {
                    IsFinished = true;
                    return;
                }

                var random = Random.Shared;
                TargetWord = validWords[random.Next(validWords.Count)];

                int difficultyInt = Preferences.Default.Get(Constants.DifficultyLevelKey, (int)DifficultyLevel.A1);
                string difficultyString = ((DifficultyLevel)difficultyInt).ToString();

                var generatedData = await _openAiService.GenerateSentenceAsync(TargetWord.Word, difficultyString);

                if (generatedData != null)
                {
                    TargetSentence = generatedData.Value.English;
                    PolishTranslation = generatedData.Value.Polish;
                }
                else
                {
                    TargetSentence = $"I like the word {TargetWord.Word}";
                    PolishTranslation = $"Lubię słowo {TargetWord.Word}";
                }

                HasAppeared.Add(TargetWord);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                await Shell.Current.DisplayAlert("Błąd", "Nie udało się załadować pytania.", "OK");
            }
            finally
            {
                IsBusy = false;
                if (!IsFinished)
                {
                    RecognizedText = "Naciśnij 'Słuchaj' i czytaj...";
                }
            }
        }

        [RelayCommand]
        private async Task StartListening()
        {
            if (IsListening || IsAnswered) return;

            var isGranted = await _speechToText.RequestPermissions(CancellationToken.None);
            if (!isGranted)
            {
                await Shell.Current.DisplayAlert("Brak uprawnień", "Potrzebujemy dostępu do mikrofonu", "OK");
                return;
            }

            _speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
            _speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;

            IsListening = true;
            RecognizedText = "Słucham...";

            try
            {
                await _speechToText.StartListenAsync(new SpeechToTextOptions
                {
                    Culture = CultureInfo.GetCultureInfo("en-US"),
                    ShouldReportPartialResults = true
                }, CancellationToken.None);
            }
            catch (FileNotFoundException)
            {
                await HandleSpeechError("Brak pakietu językowego", "Zainstaluj 'English (United States)' w ustawieniach Windows.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Privacy") || ex.Message.Contains("privacy"))
                {
                    await HandleSpeechError("Ustawienia Prywatności", "Włącz 'Rozpoznawanie mowy online' w ustawieniach Windows.");
                }
                else
                {
                    await HandleSpeechError("Błąd", $"Nie udało się uruchomić: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        public async Task StopListening()
        {
            if (!IsListening) return;

            try
            {
                await _speechToText.StopListenAsync(CancellationToken.None);
            }
            catch
            {
                // Ignorujemy błędy przy zatrzymywaniu (np. jeśli system sam już zatrzymał)
            }
            finally
            {
                await FinishAttempt();
            }
        }

        private async Task ForceStopListening()
        {
            try { await _speechToText.StopListenAsync(CancellationToken.None); } catch { }

            _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
            _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
            IsListening = false;
        }

        // real time text updates
        private void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
        {
            RecognizedText = args.RecognitionResult;
        }

        private void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
        {
            var finalResult = args.RecognitionResult.Text;
            if (string.IsNullOrEmpty(finalResult) && args.RecognitionResult.Exception == null)
            {
                finalResult = args.RecognitionResult.ToString();
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                RecognizedText = finalResult;
                //IsListening = false;
                //_speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
                //_speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
                //CheckPronunciation(RecognizedText);
                await FinishAttempt();
            });
        }

        private async Task FinishAttempt()
        {
            _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
            _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;

            IsListening = false;

            if (!string.IsNullOrWhiteSpace(RecognizedText) && RecognizedText != "Słucham... (Mów teraz)")
            {
                CheckPronunciation(RecognizedText);
            }
            else
            {
                RecognizedText = "Nie usłyszałem nic. Spróbuj ponownie.";
            }
        }

        private void CheckPronunciation(string spokenText)
        {
            if (IsAnswered) return;

            if (string.IsNullOrWhiteSpace(spokenText) || spokenText == "Słucham..." || spokenText == "Naciśnij mikrofon i czytaj...")
                return;

            IsAnswered = true;

            double similarity = CalculateSimilarity(TargetSentence, spokenText);

            if (similarity >= 80)
            {
                Score++;
                FeedbackMessage = $"Świetnie! ({similarity:F0}% zgodności)";
                FeedbackColor = Colors.Green;
            }
            else
            {
                FeedbackMessage = $"Niestety. Zgodność: {similarity:F0}%";
                FeedbackColor = Colors.Red;
            }
        }

        [RelayCommand]
        private async Task NextQuestionAsync()
        {
            await LoadQuestionAsync();
            if (IsFinished)
            { }
        }

        [RelayCommand]
        public async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        private double CalculateSimilarity(string target, string spoken)
        {
            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(spoken))
                return 0.0;

            List<string> Tokenize(string text)
            {
                return text.ToLower()
                    .Split(new[] { ' ', '.', ',', '?', '!', ';', ':', '-', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }

            var targetWords = Tokenize(target);
            var spokenWords = Tokenize(spoken);

            if (targetWords.Count == 0) return 0.0;

            var spokenPool = new List<string>(spokenWords);
            int matchCount = 0;

            foreach (var word in targetWords)
            {
                if (spokenPool.Contains(word))
                {
                    matchCount++;
                    spokenPool.Remove(word);
                }
            }

            double percentage = (double)matchCount / targetWords.Count * 100.0;

            return percentage;
        }

        private async Task HandleSpeechError(string title, string message)
        {
            Debug.WriteLine($"Speech Error: {message}");
            IsListening = false;
            RecognizedText = "Błąd konfiguracji.";
            _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
            _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
            await MainThread.InvokeOnMainThreadAsync(async () => await Shell.Current.DisplayAlert(title, message, "OK"));
        }
    }
}