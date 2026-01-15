using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class HangmanViewModelTests
{
    private readonly IScoringService _scoringService;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private readonly TestableHangmanViewModel _viewModel;

    public HangmanViewModelTests()
    {
        _scoringService = A.Fake<IScoringService>();
        _appUserService = A.Fake<IAppUserService>();
        _learningService = A.Fake<ILearningService>();
        _viewModel = new TestableHangmanViewModel(_scoringService, _appUserService, _learningService);
    }

    private class TestableHangmanViewModel : HangmanViewModel
    {
        public string? LastNavigatedRoute { get; private set; }
        public bool AlertShown { get; private set; }

        public TestableHangmanViewModel(IScoringService scoringService, IAppUserService appUserService, ILearningService learningService) 
            : base(scoringService, appUserService, learningService)
        {
        }

        protected override AppTheme GetApplicationTheme() => AppTheme.Light;
        protected override Color? GetColorResource(string key) => Colors.Gray;
        protected override Task ShowAlertAsync(string title, string message, string cancel)
        {
            AlertShown = true;
            return Task.CompletedTask;
        }
        protected override Task GoToAsync(string route)
        {
            LastNavigatedRoute = route;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Constructor_ShouldGenerateKeyboard()
    {
        _viewModel.Keyboard.Should().HaveCount(26); // A-Z
        _viewModel.Keyboard.First().Character.Should().Be('A');
        _viewModel.Keyboard.Last().Character.Should().Be('Z');
    }

    [Fact]
    public async Task LoadQuestionAsync_ShouldSetupWord_WhenCollectionIsValid()
    {
        // Arrange
        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { new() { Id = 1, Word = "Apple" } }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(1);
        await _viewModel.ImportCollectionAsync();

        // Act
        await _viewModel.LoadQuestionAsync();

        // Assert
        _viewModel.MaskedWord.Should().Be("_ _ _ _ _");
        _viewModel.Mistakes.Should().Be(0);
        _viewModel.IsAnswered.Should().BeFalse();
    }

    [Fact]
    public async Task GuessLetter_ShouldRevealLetter_WhenCorrect()
    {
        // Arrange
        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { new() { Id = 1, Word = "CAT" } }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(1);
        await _viewModel.ImportCollectionAsync();
        await _viewModel.LoadQuestionAsync();

        var letterA = _viewModel.Keyboard.First(k => k.Character == 'A');

        // Act
        _viewModel.GuessLetterCommand.Execute(letterA);

        // Assert
        _viewModel.MaskedWord.Should().Be("_ A _");
        letterA.IsEnabled.Should().BeFalse();
        letterA.BackgroundColor.Should().Be(Colors.LightGreen);
    }

    [Fact]
    public async Task GuessLetter_ShouldIncrementMistakes_WhenIncorrect()
    {
        // Arrange
        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { new() { Id = 1, Word = "CAT" } }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(1);
        await _viewModel.ImportCollectionAsync();
        await _viewModel.LoadQuestionAsync();

        var letterZ = _viewModel.Keyboard.First(k => k.Character == 'Z');

        // Act
        _viewModel.GuessLetterCommand.Execute(letterZ);

        // Assert
        _viewModel.Mistakes.Should().Be(1);
        _viewModel.MaskedWord.Should().Be("_ _ _");
        letterZ.BackgroundColor.Should().Be(Colors.Salmon);
    }

    [Fact]
    public async Task GuessLetter_ShouldWinGame_WhenAllLettersGuessed()
    {
        // Arrange
        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { new() { Id = 1, Word = "A" } }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(1);
        await _viewModel.ImportCollectionAsync();
        await _viewModel.LoadQuestionAsync();

        var letterA = _viewModel.Keyboard.First(k => k.Character == 'A');
        A.CallTo(() => _scoringService.CalculatePoints(GameType.Hangman, DifficultyLevel.A1)).Returns(5);

        // Act
        _viewModel.GuessLetterCommand.Execute(letterA);

        // Assert
        _viewModel.IsAnswered.Should().BeTrue();
        _viewModel.Score.Should().Be(1);
        _viewModel.PointsEarned.Should().Be(5);
        _viewModel.FeedbackMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GuessLetter_ShouldLoseGame_WhenMaxMistakesReached()
    {
        // Arrange
        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { new() { Id = 1, Word = "A" } }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(1);
        await _viewModel.ImportCollectionAsync();
        await _viewModel.LoadQuestionAsync();

        var wrongLetters = new[] { 'B', 'C', 'D', 'E', 'F', 'G' }; // 6 mistakes

        // Act
        foreach (var c in wrongLetters)
        {
            var key = _viewModel.Keyboard.First(k => k.Character == c);
            _viewModel.GuessLetterCommand.Execute(key);
        }

        // Assert
        _viewModel.IsAnswered.Should().BeTrue();
        _viewModel.Mistakes.Should().Be(6);
        _viewModel.Score.Should().Be(0);
        _viewModel.MaskedWord.Should().Be("A"); // Revealed on loss
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
