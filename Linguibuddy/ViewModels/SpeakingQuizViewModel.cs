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
    [QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
    public partial class SpeakingQuizViewModel : BaseQuizViewModel
    {
        private readonly ISpeechToText _speechToText;
        private readonly OpenAiService _openAiService;

        [ObservableProperty] private WordCollection? _selectedCollection;

        [ObservableProperty] private CollectionItem? _targetWord;

        [ObservableProperty] private List<CollectionItem> _hasAppeared;

        [ObservableProperty] private bool _isFinished;

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

            await StopListening();

            FeedbackMessage = string.Empty;
            FeedbackColor = Colors.Transparent;
            RecognizedText = "Naciśnij mikrofon i czytaj...";

            try
            {
                if (SelectedCollection == null || !SelectedCollection.Items.Any())
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
            }
        }

        [RelayCommand]
        private async Task StartListening()
        {
            if (IsListening) return;

            var isGranted = await _speechToText.RequestPermissions(CancellationToken.None);
            if (!isGranted)
            {
                await Shell.Current.DisplayAlert("Brak uprawnień", "Potrzebujemy mikrofonu", "OK");
                return;
            }

            // Subskrypcja zdarzeń
            _speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
            _speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;

            IsListening = true;
            RecognizedText = "Słucham..."; // Reset tekstu

            try
            {
                await _speechToText.StartListenAsync(new SpeechToTextOptions
                {
                    Culture = CultureInfo.GetCultureInfo("en-US"), // Ważne: angielski
                    ShouldReportPartialResults = true
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Start Listen Error: {ex.Message}");
                IsListening = false;
                _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
                _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
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
                /* Ignorujemy błędy zatrzymania */
            }
        }

        // Event: Wywoływany w trakcie mówienia (częściowe wyniki)
        private void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
        {
            // W nowym toolkit args.RecognitionResult to zazwyczaj cały dotychczasowy tekst, a nie tylko delta
            RecognizedText = args.RecognitionResult;
        }

        // Event: Wywoływany, gdy system wykryje ciszę/koniec
        private void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
        {
            // TO NAPRAWIA BŁĄD KONWERSJI:
            // W nowym toolkit args.RecognitionResult to obiekt SpeechToTextResult.
            // Musimy pobrać z niego właściwość .Text
            var finalResult = args.RecognitionResult.Text;

            // Fallback gdyby Text był null
            if (string.IsNullOrEmpty(finalResult) && args.RecognitionResult.Exception == null)
            {
                finalResult = args.RecognitionResult.ToString();
            }

            // Aktualizujemy UI na głównym wątku
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecognizedText = finalResult;
                IsListening = false;

                // Sprzątanie zdarzeń (Kluczowe!)
                _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
                _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;

                // Sprawdzenie wymowy
                CheckPronunciation(RecognizedText);
            });
        }

        private void CheckPronunciation(string spokenText)
        {
            if (IsAnswered) return; // Zapobiega podwójnemu sprawdzeniu

            if (string.IsNullOrWhiteSpace(spokenText) || spokenText == "Słucham..." || spokenText == "Naciśnij 'Słuchaj' i czytaj...") return;

            IsAnswered = true;

            // Obliczamy podobieństwo (algorytm Levenshteina na dole)
            double similarity = CalculateSimilarity(TargetSentence, spokenText);

            if (similarity >= 80) // 80% zgodności uznajemy za sukces
            {
                Score++;
                FeedbackMessage = $"Świetnie! ({similarity:F0}% zgodności)";
                FeedbackColor = Colors.Green;
            }
            else
            {
                FeedbackMessage = $"Spróbuj jeszcze raz. Zgodność: {similarity:F0}%";
                FeedbackColor = Colors.Red;
            }
        }

        [RelayCommand]
        private async Task NextQuestionAsync()
        {
            await LoadQuestionAsync();

            if (IsFinished)
            {
                string resultMsg = $"Twój wynik: {Score} / {SelectedCollection?.Items.Count ?? 0}";
                await Shell.Current.DisplayAlert("Koniec lekcji wymowy", resultMsg, "OK");
                await Shell.Current.GoToAsync("..");
            }
        }

        // Algorytm Levenshteina (taki sam jak wcześniej)
        private double CalculateSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;

            string Clean(string s) =>
                new string(s.ToLower().Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray()).Trim();

            source = Clean(source);
            target = Clean(target);

            if (source == target) return 100.0;
            if (source.Length == 0 || target.Length == 0) return 0.0;

            var distance = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i <= source.Length; distance[i, 0] = i++) ;
            for (int j = 0; j <= target.Length; distance[0, j] = j++) ;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            int stepsToSame = distance[source.Length, target.Length];
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length))) * 100.0;
        }
    }
}
