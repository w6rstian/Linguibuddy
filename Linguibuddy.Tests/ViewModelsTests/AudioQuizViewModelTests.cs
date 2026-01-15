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
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class AudioQuizViewModelTests : IDisposable
{
    private readonly IScoringService _scoringService;
    private readonly IAudioManager _audioManager;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private readonly AudioQuizViewModel _viewModel;

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

        _viewModel = new AudioQuizViewModel(_scoringService, _audioManager, _appUserService, _learningService);
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
}