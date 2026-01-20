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
public class ImageQuizViewModelTests
{
    private readonly ICollectionService _collectionService;
    private readonly IScoringService _scoringService;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private readonly TestableImageQuizViewModel _viewModel;

    public ImageQuizViewModelTests()
    {
        // Dependencies
        _collectionService = A.Fake<ICollectionService>();
        _scoringService = A.Fake<IScoringService>();
        _appUserService = A.Fake<IAppUserService>();
        _learningService = A.Fake<ILearningService>();

        _viewModel = new TestableImageQuizViewModel(_collectionService, _scoringService, _appUserService, _learningService);
    }

    private class TestableImageQuizViewModel : ImageQuizViewModel
    {
        public bool MockNetworkStatus { get; set; } = true;
        public string? LastAlertMessage { get; private set; }
        public string? LastNavigatedRoute { get; private set; }

        public TestableImageQuizViewModel(ICollectionService collectionService, IScoringService scoringService, IAppUserService appUserService, ILearningService learningService) 
            : base(collectionService, scoringService, appUserService, learningService)
        {
        }

        protected override bool IsNetworkConnected() => MockNetworkStatus;

        protected override Task ShowAlert(string title, string message, string cancel)
        {
            LastAlertMessage = message;
            return Task.CompletedTask;
        }

        protected override Task GoToAsync(string route)
        {
            LastNavigatedRoute = route;
            return Task.CompletedTask;
        }
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
    public async Task LoadQuestionAsync_ShouldPrepareOptions_WhenNetworkIsAvailable()
    {
        // Arrange
        var items = new List<CollectionItem>();
        for (int i = 1; i <= 5; i++)
        {
            items.Add(new CollectionItem { Id = i, Word = $"Word{i}" });
        }

        var collection = new WordCollection { Items = items };
        _viewModel.SelectedCollection = collection;

        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(5);
        await _viewModel.ImportCollectionAsync();

        // Act
        await _viewModel.LoadQuestionAsync();

        // Assert
        _viewModel.TargetWord.Should().NotBeNull();
        _viewModel.Options.Should().HaveCount(4);
        _viewModel.Options.Select(o => o.Word).Should().Contain(_viewModel.TargetWord);
    }

    [Fact]
    public async Task LoadQuestionAsync_ShouldShowAlert_WhenNetworkIsUnavailable()
    {
        // Arrange
        _viewModel.MockNetworkStatus = false;

        // Act
        await _viewModel.LoadQuestionAsync();

        // Assert
        _viewModel.LastAlertMessage.Should().NotBeNullOrEmpty();
        _viewModel.LastNavigatedRoute.Should().Be("..");
    }

    [Fact]
    public async Task SelectAnswerCommand_ShouldIncrementScore_WhenAnswerIsCorrect()
    {
        // Arrange
        var targetWord = new CollectionItem { Id = 1, Word = "Target" };
        var option = new QuizOption(targetWord);
        
        _viewModel.TargetWord = targetWord;
        _viewModel.Options.Add(option);

        A.CallTo(() => _scoringService.CalculatePoints(GameType.ImageQuiz, DifficultyLevel.A1)).Returns(10);

        // Act
        _viewModel.SelectAnswerCommand.Execute(option);

        // Assert
        _viewModel.Score.Should().Be(1);
        _viewModel.PointsEarned.Should().Be(10);
        option.BackgroundColor.Should().Be(Colors.LightGreen);
    }

    [Fact]
    public void SelectAnswerCommand_ShouldMarkIncorrect_WhenAnswerIsWrong()
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
        _viewModel.SelectAnswerCommand.Execute(wrongOption);

        // Assert
        _viewModel.Score.Should().Be(0);
        wrongOption.BackgroundColor.Should().Be(Colors.Salmon);
        correctOption.BackgroundColor.Should().Be(Colors.LightGreen);
    }

    [Fact]
    public async Task GoBack_ShouldNavigateBack()
    {
        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be("..");
    }

    [Fact]
    public async Task ImportCollectionAsync_ShouldFilterDuplicates_AndPrioritizeImage()
    {
        // Arrange
        var itemWithImage = new CollectionItem { Id = 1, Word = "fork", ImageUrl = "http://image.jpg" };
        var itemWithoutImage = new CollectionItem { Id = 2, Word = "Fork", ImageUrl = "" };
        var itemOther = new CollectionItem { Id = 3, Word = "Spoon" };

        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { itemWithoutImage, itemWithImage, itemOther }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(10);

        // Act
        await _viewModel.ImportCollectionAsync();

        // Check internal 'allWords' using reflection
        var field = typeof(ImageQuizViewModel).GetField("_allWords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var allWords = (List<CollectionItem>)field.GetValue(_viewModel);

        // Assert
        allWords.Should().HaveCount(2); // fork + Spoon
        allWords.Should().Contain(i => i.Word.Equals("fork", StringComparison.OrdinalIgnoreCase));
        allWords.Should().Contain(i => i.Word == "Spoon");
        
        // Verify image priority
        var forkItem = allWords.First(i => i.Word.Equals("fork", StringComparison.OrdinalIgnoreCase));
        forkItem.ImageUrl.Should().Be("http://image.jpg");
    }
}