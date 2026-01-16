using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Plugin.Maui.Audio;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Linguibuddy.ViewModels;

[QueryProperty(nameof(SelectedCollection), "SelectedCollection")]
public partial class AudioQuizViewModel : BaseQuizViewModel
{
    private readonly IAudioManager _audioManager;
    private readonly IScoringService _scoringService;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private IAudioPlayer? _audioPlayer;
    private List<CollectionItem> allWords;
    private Random random = Random.Shared;

    [ObservableProperty] private List<CollectionItem> _hasAppeared;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsLearning))]
    private bool _isFinished;

    [ObservableProperty] private int _pointsEarned;

    [ObservableProperty] private int _score;

    [ObservableProperty] private WordCollection? _selectedCollection;

    [ObservableProperty] private CollectionItem? _targetWord;

    public AudioQuizViewModel(IScoringService scoringService, IAudioManager audioManager, IAppUserService appUserService, ILearningService learningService)
    {
        _scoringService = scoringService;
        _audioManager = audioManager;
        _appUserService = appUserService;
        _learningService = learningService;

        HasAppeared = [];
        IsFinished = false;
        Score = 0;
        PointsEarned = 0;
    }

    public bool IsLearning => !IsFinished;

    public ObservableCollection<QuizOption> Options { get; } = [];

    public async Task ImportCollectionAsync()
    {
        if (SelectedCollection is null || !SelectedCollection.Items.Any())
            return;

        allWords = SelectedCollection.Items
                .GroupBy(i => i.Word, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.FirstOrDefault(i => !string.IsNullOrEmpty(i.Audio)) ?? g.First())
                .OrderBy(_ => random.Next())
                .Take(await _appUserService.GetUserLessonLengthAsync())
                .ToList();
    }
    public override async Task LoadQuestionAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsAnswered = false;
        FeedbackMessage = string.Empty;
        FeedbackColor =
            Application.Current.RequestedTheme == AppTheme.Light
                ? Application.Current.Resources["PrimaryDarkText"] as Color
                : Colors.White;
        Options.Clear();

        if (!IsNetworkConnected())
        {
            await ShowAlert(AppResources.NetworkError, AppResources.NetworkRequired, "OK");
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

            // We only choose allWords that have not been asked as the next word. All allWords can appear as an incorrect QuizOption.

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

            foreach (var word in optionsList) Options.Add(new QuizOption(word));

            HasAppeared.Add(TargetWord);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading quiz: {ex.Message}");
            await ShowAlert(AppResources.Error, AppResources.FailedLoadQuestion, "OK");
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
            Score++;

            var points = _scoringService.CalculatePoints(GameType.AudioQuiz, DifficultyLevel.A1);
            PointsEarned += points;

            FeedbackMessage = AppResources.CorrectAnswer;
            FeedbackColor = Colors.Green;
        }
        else
        {
            selectedOption.BackgroundColor = Colors.Salmon;
            FeedbackMessage = $"{AppResources.IncorrectAnswer} {TargetWord.Word}";
            FeedbackColor = Colors.Red;

            var correctOption = Options.FirstOrDefault(o => o.Word.Id == TargetWord.Id);
            if (correctOption != null) correctOption.BackgroundColor = Colors.LightGreen;
        }
    }

    [RelayCommand]
    private async Task PlayAudioAsync()
    {
        if (TargetWord == null || string.IsNullOrWhiteSpace(TargetWord.Audio)) return;

        var url = TargetWord.Audio;

        if (_audioPlayer != null && _audioPlayer.IsPlaying) _audioPlayer.Dispose();

        try
        {
            using var client = new HttpClient();
            var audioBytes = await client.GetByteArrayAsync(url);

            var fileName = "quiz_temp_audio.mp3";
            var filePath = Path.Combine(GetCacheDirectory(), fileName);

            await File.WriteAllBytesAsync(filePath, audioBytes);

            var fileStream = File.OpenRead(filePath);

            _audioPlayer = _audioManager.CreatePlayer(fileStream);
            _audioPlayer.Play();

            _audioPlayer.PlaybackEnded += (s, e) => { fileStream.Dispose(); };
        }
        catch (Exception ex)
        {
            await ShowAlert(AppResources.AudioError, AppResources.PlaybackError, "OK");
            Debug.WriteLine($"Audio Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task GoBack()
    {
        await GoToAsync("..");
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
                    GameType.AudioQuiz,
                    Score,
                    allWords.Count,
                    PointsEarned
                );
            }
    }

    protected virtual bool IsNetworkConnected()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }

    protected virtual Task ShowAlert(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }

    protected virtual string GetCacheDirectory()
    {
        return FileSystem.CacheDirectory;
    }

    protected virtual Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }
}