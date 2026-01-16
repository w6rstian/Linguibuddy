using CommunityToolkit.Maui.Media;
using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class SpeakingQuizViewModelTests
{
    private readonly ISpeechToText _speechToText;
    private readonly IOpenAiService _openAiService;
    private readonly IScoringService _scoringService;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private readonly TestableSpeakingQuizViewModel _viewModel;

    public SpeakingQuizViewModelTests()
    {
        _speechToText = A.Fake<ISpeechToText>();
        _openAiService = A.Fake<IOpenAiService>();
        _scoringService = A.Fake<IScoringService>();
        _appUserService = A.Fake<IAppUserService>();
        _learningService = A.Fake<ILearningService>();
        _viewModel = new TestableSpeakingQuizViewModel(_speechToText, _openAiService, _scoringService, _appUserService, _learningService);
    }

    private class TestableSpeakingQuizViewModel : SpeakingQuizViewModel
    {
        public bool MockNetworkStatus { get; set; } = true;
        public string? LastAlertMessage { get; private set; }
        public string? LastNavigatedRoute { get; private set; }
        public string? LastSpokenText { get; private set; }

        public TestableSpeakingQuizViewModel(ISpeechToText speechToText, IOpenAiService openAiService, IScoringService scoringService, IAppUserService appUserService, ILearningService learningService) 
            : base(speechToText, openAiService, scoringService, appUserService, learningService)
        {
        }

        protected override bool IsNetworkConnected() => MockNetworkStatus;

        protected override Task ShowAlertAsync(string title, string message, string cancel)
        {
            LastAlertMessage = message;
            return Task.CompletedTask;
        }

        protected override Task GoToAsync(string route)
        {
            LastNavigatedRoute = route;
            return Task.CompletedTask;
        }

        protected override Task SpeakAsync(string text, CancellationToken token)
        {
            LastSpokenText = text;
            return Task.CompletedTask;
        }

        protected override void RunOnMainThread(Action action) => action();

        protected override Task InvokeOnMainThreadAsync(Func<Task> action) => action();

        // Expose protected methods for testing
        public void CallCheckPronunciation(string text) => base.CheckPronunciation(text);
        public Task CallFinishAttempt() => base.FinishAttempt();
    }

    [Fact]
    public async Task ImportCollectionAsync_ShouldCallGetUserLessonLengthAsync_WhenCollectionIsValid()
    {
        // Arrange
        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { new() { Id = 1, Word = "Test" } }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(1);

        // Act
        await _viewModel.ImportCollectionAsync();

        // Assert
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task LoadQuestionAsync_ShouldSetupTargetSentence_WhenNetworkAvailable()
    {
        // Arrange
        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { new() { Id = 1, Word = "Apple" } }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(1);
        await _viewModel.ImportCollectionAsync();

        A.CallTo(() => _appUserService.GetUserDifficultyAsync()).Returns(DifficultyLevel.A1);
        A.CallTo(() => _openAiService.GenerateSentenceAsync("Apple", "A1", A<string>.Ignored))
            .Returns(("I eat an apple", "Jem jabłko"));

        // Act
        await _viewModel.LoadQuestionAsync();

        // Assert
        _viewModel.TargetSentence.Should().Be("I eat an apple");
        _viewModel.PolishTranslation.Should().Be("Jem jabłko");
        _viewModel.IsAnswered.Should().BeFalse();
    }

    [Fact]
    public async Task StartListening_ShouldSetIsListening_WhenPermissionGranted()
    {
        // Arrange
        A.CallTo(() => _speechToText.RequestPermissions(A<CancellationToken>.Ignored)).Returns(true);

        // Act
        await _viewModel.StartListeningCommand.ExecuteAsync(null);

        // Assert
        _viewModel.IsListening.Should().BeTrue();
        A.CallTo(() => _speechToText.StartListenAsync(A<SpeechToTextOptions>.Ignored, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CheckPronunciation_ShouldScorePoint_WhenSimilarityIsHigh()
    {
        // Arrange
        await SetupQuiz("I eat an apple");
        A.CallTo(() => _scoringService.CalculatePoints(GameType.SpeakingQuiz, DifficultyLevel.A1)).Returns(10);

        // Act
        _viewModel.CallCheckPronunciation("I eat an apple");

        // Assert
        _viewModel.Score.Should().Be(1);
        _viewModel.PointsEarned.Should().Be(10);
        _viewModel.IsAnswered.Should().BeTrue();
        _viewModel.FeedbackColor.Should().Be(Colors.Green);
    }

    [Fact]
    public async Task CheckPronunciation_ShouldFail_WhenSimilarityIsLow()
    {
        // Arrange
        await SetupQuiz("I eat an apple");

        // Act
        _viewModel.CallCheckPronunciation("Something else entirely");

        // Assert
        _viewModel.Score.Should().Be(0);
        _viewModel.IsAnswered.Should().BeTrue();
        _viewModel.FeedbackColor.Should().Be(Colors.Red);
    }

    [Fact]
    public async Task FinishAttempt_ShouldHandleEmptyRecognition()
    {
        // Arrange
        await SetupQuiz("I eat an apple");
        _viewModel.RecognizedText = "";

        // Act
        await _viewModel.CallFinishAttempt();

        // Assert
        _viewModel.IsAnswered.Should().BeFalse();
        _viewModel.RecognizedText.Should().Contain("Nie usłyszałem");
    }

    [Fact]
    public async Task GoBack_ShouldNavigateBack()
    {
        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be("..");
    }

    private async Task SetupQuiz(string sentence)
    {
        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { new() { Id = 1, Word = "Apple" } }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(1);
        await _viewModel.ImportCollectionAsync();

        A.CallTo(() => _appUserService.GetUserDifficultyAsync()).Returns(DifficultyLevel.A1);
        A.CallTo(() => _openAiService.GenerateSentenceAsync("Apple", "A1", A<string>.Ignored))
            .Returns((sentence, "Translation"));

        await _viewModel.LoadQuestionAsync();
    }
}