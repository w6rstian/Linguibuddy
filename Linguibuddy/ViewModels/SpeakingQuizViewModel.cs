using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using System;
using System.Diagnostics;
using System.Globalization;
using Linguibuddy.Interfaces;

namespace Linguibuddy.ViewModels;

// TODO: add translations and better view
[QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
public partial class SpeakingQuizViewModel : BaseQuizViewModel
{
    private readonly IAppUserService _appUserService;
    private readonly IOpenAiService _openAiService;
    private readonly IScoringService _scoringService;
    private readonly ISpeechToText _speechToText;
    private readonly ILearningService _learningService;
    private List<CollectionItem> allWords;
    private Random random = Random.Shared;

    private CancellationTokenSource? _ttsCts;

    private DifficultyLevel _currentDifficulty;
    [ObservableProperty] private List<CollectionItem> _hasAppeared;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLearning))]
    private bool _isFinished;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsNotListening))]
    private bool _isListening;

    [ObservableProperty] private int _pointsEarned;
    [ObservableProperty] private string _polishTranslation;

    [ObservableProperty] private string _recognizedText;

    [ObservableProperty] private int _score;

    [ObservableProperty] private WordCollection? _selectedCollection;
    [ObservableProperty] private string _targetSentence;
    [ObservableProperty] private CollectionItem? _targetWord;

    public SpeakingQuizViewModel(ISpeechToText speechToText,
        IOpenAiService openAiService,
        IScoringService scoringService,
        IAppUserService appUserService,
        ILearningService learningService)
    {
        _speechToText = speechToText;
        _openAiService = openAiService;
        _scoringService = scoringService;
        _appUserService = appUserService;
        _learningService = learningService;

        HasAppeared = [];
        IsFinished = false;
        Score = 0;
        PointsEarned = 0;
        RecognizedText = AppResources.SentenceRead;
    }

    public bool IsLearning => !IsFinished;
    public bool IsNotListening => !IsListening;

    public async Task ImportCollectionAsync()
    {
        if (SelectedCollection is null || !SelectedCollection.Items.Any())
            return;

        allWords = SelectedCollection.Items
                .OrderBy(_ => random.Next())
                .Take(await _appUserService.GetUserLessonLengthAsync())
                .ToList();
    }

    public override async Task LoadQuestionAsync()
    {
        StopTTS();
        await ForceStopListening();

        _currentDifficulty = await _appUserService.GetUserDifficultyAsync();

        if (IsBusy) return;
        IsBusy = true;
        IsAnswered = false;

        FeedbackMessage = string.Empty;
        FeedbackColor = Colors.Transparent;
        PolishTranslation = AppResources.SentenceGenerating;
        TargetSentence = string.Empty;
        RecognizedText = string.Empty;

        if (!IsNetworkConnected())
        {
            await ShowAlertAsync(AppResources.NetworkError, AppResources.NetworkRequired, "OK");
            IsBusy = false;
            return;
        }

        try
        {
            if (SelectedCollection == null || SelectedCollection.Items == null || !SelectedCollection.Items.Any())
            {
                IsFinished = true;
                return;
            }

            var validWords = allWords.Except(HasAppeared).ToList();

            if (validWords.Count == 0)
            {
                IsFinished = true;
                return;
            }

            TargetWord = validWords[random.Next(validWords.Count)];

            //int difficultyInt = Preferences.Default.Get(Constants.DifficultyLevelKey, (int)DifficultyLevel.A1);
            var difficultyString = _currentDifficulty.ToString();

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
            await ShowAlertAsync(AppResources.Error, AppResources.FailedLoadQuestion, "OK");
        }
        finally
        {
            IsBusy = false;
            if (!IsFinished)
            {
                RecognizedText = AppResources.SentenceRead;
                _ = ReadTargetSentence(TargetSentence);
            }
        }
    }

    [RelayCommand]
    private async Task StartListening()
    {
        if (IsListening || IsAnswered) return;

        StopTTS();

        var isGranted = await _speechToText.RequestPermissions(CancellationToken.None);
        if (!isGranted)
        {
            await ShowAlertAsync(AppResources.NoPermissions, AppResources.MicrophoneNeeded, "OK");
            return;
        }

        _speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;

        IsListening = true;
        RecognizedText = AppResources.Listening;

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
            await HandleSpeechError(AppResources.Error, AppResources.InstallEng);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Privacy") || ex.Message.Contains("privacy"))
                await HandleSpeechError(AppResources.Error, AppResources.OnlineSpeech);
            else
                await HandleSpeechError(AppResources.Error, AppResources.Error + ex.Message);
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
            // Ignorujemy błędy przy zatrzymywaniu
        }
        finally
        {
            await FinishAttempt();
        }
    }

    // do odtworzenia TTS na razie niepotrzebne
    [RelayCommand]
    public async Task PlayAudio()
    {
        if (!string.IsNullOrEmpty(TargetSentence))
        {
            await ReadTargetSentence(TargetSentence);
        }
    }

    [RelayCommand]
    public async Task ReadTargetSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // zatrzymanie mikrofonu
        if (IsListening)
        {
            await StopListening();
        }

        StopTTS();

        _ttsCts = new CancellationTokenSource();

        try
        {
            await SpeakAsync(text, _ttsCts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Mowa została przerwana.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TTS Error: {ex.Message}");
        }
        finally
        {
            if (_ttsCts != null)
            {
                _ttsCts.Dispose();
                _ttsCts = null;
            }
        }
    }

    public void StopTTS()
    {
        if (_ttsCts != null && !_ttsCts.IsCancellationRequested)
        {
            _ttsCts.Cancel();
        }
    }

    private async Task ForceStopListening()
    {
        try
        {
            await _speechToText.StopListenAsync(CancellationToken.None);
        }
        catch
        {
            // ignored
        }

        _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
        IsListening = false;
    }

    // real time update tekstu
    private void OnRecognitionTextUpdated(object? sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
    {
        RecognizedText = args.RecognitionResult;
    }

    private void OnRecognitionTextCompleted(object? sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    {
        var finalResult = args.RecognitionResult.Text;

        RunOnMainThread(async () =>
        {
            RecognizedText = finalResult;
            //IsListening = false;
            //_speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
            //_speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
            //CheckPronunciation(RecognizedText);
            await FinishAttempt();
        });
    }

    protected virtual async Task FinishAttempt()
    {
        _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;

        IsListening = false;

        if (!string.IsNullOrWhiteSpace(RecognizedText) && RecognizedText != AppResources.Listening)
            CheckPronunciation(RecognizedText);
        else
            RecognizedText = "Nie usłyszałem nic. Spróbuj ponownie.";
    }

    protected virtual void CheckPronunciation(string spokenText)
    {
        if (IsAnswered) return;

        if (string.IsNullOrWhiteSpace(spokenText) || spokenText == AppResources.Listening)
            return;

        IsAnswered = true;

        var similarity = CalculateSimilarity(TargetSentence, spokenText);

        if (similarity >= 80)
        {
            Score++;

            //int difficultyInt = Preferences.Default.Get(Constants.DifficultyLevelKey, (int)DifficultyLevel.A1);
            //var difficulty = (DifficultyLevel)difficultyInt;

            var points = _scoringService.CalculatePoints(GameType.SpeakingQuiz, _currentDifficulty);
            PointsEarned += points;

            FeedbackMessage = AppResources.YouDidIt;
            FeedbackColor = Colors.Green;
        }
        else
        {
            FeedbackMessage = AppResources.TryHarder;
            FeedbackColor = Colors.Red;
        }
    }

    [RelayCommand]
    private async Task NextQuestionAsync()
    {
        await LoadQuestionAsync();
        if (IsFinished)
            if (SelectedCollection != null)
            {
                await _learningService.MarkLearnedTodayAsync();
                await _scoringService.SaveResultsAsync(
                    SelectedCollection,
                    GameType.SpeakingQuiz,
                    Score,
                    allWords.Count,
                    PointsEarned
                );
            }    
    }

    [RelayCommand]
    public async Task GoBack()
    {
        await GoToAsync("..");
    }

    private double CalculateSimilarity(string target, string spoken)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(spoken))
            return 0.0;

        List<string> Tokenize(string text)
        {
            return text.ToLower()
                .Split(new[] { ' ', '.', ',', '?', '!', ';', ':', '-', '"', '\'' },
                    StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        var targetWords = Tokenize(target);
        var spokenWords = Tokenize(spoken);

        if (targetWords.Count == 0) return 0.0;

        var spokenPool = new List<string>(spokenWords);
        var matchCount = 0;

        foreach (var word in targetWords)
            if (spokenPool.Contains(word))
            {
                matchCount++;
                spokenPool.Remove(word);
            }

        var percentage = (double)matchCount / targetWords.Count * 100.0;

        return percentage;
    }

    private async Task HandleSpeechError(string title, string message)
    {
        Debug.WriteLine($"Speech Error: {message}");
        IsListening = false;
        RecognizedText = AppResources.Error;
        _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
        await InvokeOnMainThreadAsync(async () => await ShowAlertAsync(title, message, "OK"));
    }

    protected virtual bool IsNetworkConnected()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }

    protected virtual Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }

    protected virtual Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }

    protected virtual async Task SpeakAsync(string text, CancellationToken token)
    {
        var locales = await TextToSpeech.Default.GetLocalesAsync();
        string[] femaleVoices = { "Zira", "Paulina", "Jenny", "Aria" };
        var preferred = locales.FirstOrDefault(l =>
                            (l.Language == "en-US" || (l.Language == "en" && l.Country == "US")) &&
                            femaleVoices.Any(f => l.Name.Contains(f)))
                        ?? locales.FirstOrDefault(l =>
                            (l.Language == "en-GB" || (l.Language == "en" && l.Country == "GB")) &&
                            femaleVoices.Any(f => l.Name.Contains(f)))
                        ?? locales.FirstOrDefault(l =>
                            l.Language.StartsWith("en") && femaleVoices.Any(f => l.Name.Contains(f)))
                        ?? locales.FirstOrDefault(l =>
                            l.Language == "en-US" || (l.Language == "en" && l.Country == "US"))
                        ?? locales.FirstOrDefault(l =>
                            l.Language == "en-GB" || (l.Language == "en" && l.Country == "GB"))
                        ?? locales.FirstOrDefault(l => l.Language.StartsWith("en"));

        if (preferred == null)
        {
            await ShowAlertAsync(AppResources.Error, AppResources.InstallEng, "OK");
            return;
        }

        await TextToSpeech.Default.SpeakAsync(text, new SpeechOptions
        {
            Locale = preferred,
            Pitch = 1.0f,
            Volume = 1.0f
        }, cancelToken: token);
    }

    protected virtual void RunOnMainThread(Action action)
    {
        MainThread.BeginInvokeOnMainThread(action);
    }

    protected virtual Task InvokeOnMainThreadAsync(Func<Task> action)
    {
        return MainThread.InvokeOnMainThreadAsync(action);
    }
}