using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

[Collection("QuizTests")]
public class SentenceQuizViewModelTests
{
    private readonly IOpenAiService _openAiService;
    private readonly IScoringService _scoringService;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private readonly TestableSentenceQuizViewModel _viewModel;

    public SentenceQuizViewModelTests()
    {
        _openAiService = A.Fake<IOpenAiService>();
        _scoringService = A.Fake<IScoringService>();
        _appUserService = A.Fake<IAppUserService>();
        _learningService = A.Fake<ILearningService>();
        _viewModel = new TestableSentenceQuizViewModel(_openAiService, _scoringService, _appUserService, _learningService);
    }

    private class TestableSentenceQuizViewModel : SentenceQuizViewModel
    {
        public bool MockNetworkStatus { get; set; } = true;
        public string? LastAlertMessage { get; private set; }
        public string? LastNavigatedRoute { get; private set; }
        public string? LastSpokenText { get; private set; }

        public TestableSentenceQuizViewModel(IOpenAiService openAiService, IScoringService scoringService, IAppUserService appUserService, ILearningService learningService) 
            : base(openAiService, scoringService, appUserService, learningService)
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

        protected override Task SpeakAsync(string text)
        {
            LastSpokenText = text;
            return Task.CompletedTask;
        }

        protected override AppTheme GetApplicationTheme() => AppTheme.Light;
        protected override Color? GetColorResource(string key) => Colors.Gray;
    }

    [Fact]
    public async Task LoadQuestionAsync_ShouldNavigateBack_WhenNetworkIsNotAvailable()
    {
        // Arrange
        _viewModel.MockNetworkStatus = false;

        // Act
        await _viewModel.LoadQuestionAsync();

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be("..");
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
    public async Task LoadQuestionAsync_ShouldPrepareQuestion_WhenNetworkAvailable()
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
        _viewModel.TargetWord.Word.Should().Be("Apple");
        _viewModel.AvailableWords.Should().Contain(w => w.Text == "eat");
        _viewModel.AvailableWords.Should().Contain(w => w.Text == "apple");
    }

    [Fact]
    public async Task SelectWord_ShouldMoveWordToSelected()
    {
        // Arrange
        await SetupQuiz();
        var wordTile = _viewModel.AvailableWords.First();

        // Act
        _viewModel.SelectWordCommand.Execute(wordTile);

        // Assert
        _viewModel.AvailableWords.Should().NotContain(wordTile);
        _viewModel.SelectedWords.Should().Contain(wordTile);
    }

    [Fact]
    public async Task DeselectWord_ShouldMoveWordBackToAvailable()
    {
        // Arrange
        await SetupQuiz();
        var wordTile = _viewModel.AvailableWords.First();
        _viewModel.SelectWordCommand.Execute(wordTile);

        // Act
        _viewModel.DeselectWordCommand.Execute(wordTile);

        // Assert
        _viewModel.SelectedWords.Should().NotContain(wordTile);
        _viewModel.AvailableWords.Should().Contain(wordTile);
    }

    [Fact]
    public async Task CheckAnswer_ShouldScorePoint_WhenCorrect()
    {
        // Arrange
        await SetupQuiz();
        
        var wordsToSelect = new[] { "I", "eat", "an", "apple" };
        foreach (var text in wordsToSelect)
        {
            var tile = _viewModel.AvailableWords.First(w => w.Text == text);
            _viewModel.SelectWordCommand.Execute(tile);
        }

        A.CallTo(() => _scoringService.CalculatePoints(GameType.SentenceQuiz, DifficultyLevel.A1)).Returns(10);

        // Act
        _viewModel.CheckAnswerCommand.Execute(null);

        // Assert
        _viewModel.Score.Should().Be(1);
        _viewModel.PointsEarned.Should().Be(10);
        _viewModel.IsAnswered.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAnswer_ShouldFail_WhenIncorrect()
    {
        // Arrange
        await SetupQuiz();
        
        var tile = _viewModel.AvailableWords.First(w => w.Text == "apple");
        _viewModel.SelectWordCommand.Execute(tile);

        // Act
        _viewModel.CheckAnswerCommand.Execute(null);

        // Assert
        _viewModel.Score.Should().Be(0);
        _viewModel.IsAnswered.Should().BeTrue();
    }

    [Fact]
    public async Task GoBack_ShouldNavigateBack()
    {
        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be("..");
    }

    private async Task SetupQuiz()
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
            .Returns(("I eat an apple", "Jem jabłko"));

        await _viewModel.LoadQuestionAsync();
    }
}