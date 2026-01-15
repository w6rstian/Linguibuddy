using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class AudioQuizViewModelTests : IDisposable
{
    private readonly IScoringService _scoringService;
    private readonly IAudioManager _audioManager;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private TestableAudioQuizViewModel _viewModel;

    // Testable subclass to bypass static MAUI dependencies
    private class TestableAudioQuizViewModel : AudioQuizViewModel
    {
        public string LastNavigatedRoute { get; private set; } = string.Empty;

        public TestableAudioQuizViewModel(IScoringService scoringService, IAudioManager audioManager, IAppUserService appUserService, ILearningService learningService) 
            : base(scoringService, audioManager, appUserService, learningService)
        {
        }

        protected override bool IsNetworkConnected()
        {
            return true; // Simulate network available
        }

        protected override Task ShowAlert(string title, string message, string cancel)
        {
            return Task.CompletedTask; // Bypass UI alert
        }
        
        protected override Task GoToAsync(string route)
        {
            LastNavigatedRoute = route;
            return Task.CompletedTask;
        }

        protected override string GetCacheDirectory()
        {
             return System.IO.Path.GetTempPath();
        }
    }

    public AudioQuizViewModelTests()
    {
        // Setup Application.Current for QuizOption constructor dependencies
        var app = A.Fake<Application>();
        var resources = new ResourceDictionary
        {
            { "Primary", Colors.Blue },
            { "PrimaryDarkText", Colors.Black }
        };
        
        app.Resources = resources;
        Application.Current = app;

        _scoringService = A.Fake<IScoringService>();
        _audioManager = A.Fake<IAudioManager>();
        _appUserService = A.Fake<IAppUserService>();
        _learningService = A.Fake<ILearningService>();

        _viewModel = new TestableAudioQuizViewModel(_scoringService, _audioManager, _appUserService, _learningService);
    }

    public void Dispose()
    {
        Application.Current = null;
    }

    [Fact]
    public void Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Assert
        _viewModel.HasAppeared.Should().BeEmpty();
        _viewModel.IsFinished.Should().BeFalse();
        _viewModel.Score.Should().Be(0);
        _viewModel.PointsEarned.Should().Be(0);
        _viewModel.IsLearning.Should().BeTrue();
        _viewModel.Options.Should().BeEmpty();
    }

    [Fact]
    public async Task ImportCollectionAsync_ShouldCallGetUserLessonLengthAsync_WhenCollectionIsValid()
    {
        // Arrange
        var collection = new WordCollection
        {
            Items = new List<CollectionItem>
            {
                new CollectionItem { Id = 1, Word = "Test1" },
                new CollectionItem { Id = 2, Word = "Test2" }
            }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(10);

        // Act
        await _viewModel.ImportCollectionAsync();

        // Assert
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ImportCollectionAsync_ShouldNotCallService_WhenCollectionIsNull()
    {
        // Arrange
        _viewModel.SelectedCollection = null;

        // Act
        await _viewModel.ImportCollectionAsync();

        // Assert
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).MustNotHaveHappened();
    }

    [Fact]
    public async Task LoadQuestionAsync_ShouldPrepareOptions_WhenNetworkIsAvailable()
    {
        // Arrange
        var items = new List<CollectionItem>();
        // We need at least 4 items for the quiz to proceed
        for (int i = 1; i <= 5; i++)
        {
            items.Add(new CollectionItem { Id = i, Word = $"Word{i}" });
        }

        var collection = new WordCollection { Items = items };
        _viewModel.SelectedCollection = collection;

        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(5);
        
        // Populate the internal 'allWords' list
        await _viewModel.ImportCollectionAsync();

        // Act
        await _viewModel.LoadQuestionAsync();

        // Assert
        _viewModel.TargetWord.Should().NotBeNull();
        _viewModel.Options.Should().HaveCount(4);
        _viewModel.Options.Select(o => o.Word).Should().Contain(_viewModel.TargetWord);
        _viewModel.HasAppeared.Should().Contain(_viewModel.TargetWord);
    }

    [Fact]
    public async Task SelectAnswerCommand_ShouldIncrementScoreAndPoints_WhenAnswerIsCorrect()
    {
        // Arrange
        var targetWord = new CollectionItem { Id = 1, Word = "Target" };
        var option = new QuizOption(targetWord);
        
        _viewModel.TargetWord = targetWord;
        _viewModel.Options.Add(option);

        A.CallTo(() => _scoringService.CalculatePoints(GameType.AudioQuiz, DifficultyLevel.A1)).Returns(10);

        // Act
        await _viewModel.SelectAnswerCommand.ExecuteAsync(option);

        // Assert
        _viewModel.Score.Should().Be(1);
        _viewModel.PointsEarned.Should().Be(10);
        option.BackgroundColor.Should().Be(Colors.LightGreen);
    }

    [Fact]
    public async Task SelectAnswerCommand_ShouldMarkIncorrectAndFindCorrect_WhenAnswerIsIncorrect()
    {
        // Arrange
        var targetWord = new CollectionItem { Id = 1, Word = "Target" };
        var wrongWord = new CollectionItem { Id = 2, Word = "Wrong" };
        
        var correctOption = new QuizOption(targetWord);
        var wrongOption = new QuizOption(wrongWord);
        
        _viewModel.TargetWord = targetWord;
        _viewModel.Options.Add(correctOption);
        _viewModel.Options.Add(wrongOption);

        // Act
        await _viewModel.SelectAnswerCommand.ExecuteAsync(wrongOption);

        // Assert
        _viewModel.Score.Should().Be(0);
        wrongOption.BackgroundColor.Should().Be(Colors.Salmon);
        correctOption.BackgroundColor.Should().Be(Colors.LightGreen);
    }
    
    [Fact]
    public async Task SelectAnswerCommand_ShouldDoNothing_WhenAlreadyAnswered()
    {
        // Arrange
        var targetWord = new CollectionItem { Id = 1, Word = "Target" };
        var option = new QuizOption(targetWord);
        
        _viewModel.TargetWord = targetWord;
        _viewModel.IsAnswered = true;

        // Act
        await _viewModel.SelectAnswerCommand.ExecuteAsync(option);

        // Assert
        _viewModel.Score.Should().Be(0);
    }

    [Fact]
    public async Task GoBack_ShouldNavigateBack()
    {
        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be("..");
    }
}
