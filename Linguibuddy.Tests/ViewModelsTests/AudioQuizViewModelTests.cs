using System.Reflection;
using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using Plugin.Maui.Audio;

namespace Linguibuddy.Tests.ViewModelsTests;

[Collection("QuizTests")]
public class AudioQuizViewModelTests
{
    private readonly IAppUserService _appUserService;
    private readonly IAudioManager _audioManager;
    private readonly ILearningService _learningService;
    private readonly IScoringService _scoringService;
    private readonly TestableAudioQuizViewModel _viewModel;

    public AudioQuizViewModelTests()
    {
        _scoringService = A.Fake<IScoringService>();
        _audioManager = A.Fake<IAudioManager>();
        _appUserService = A.Fake<IAppUserService>();
        _learningService = A.Fake<ILearningService>();

        _viewModel = new TestableAudioQuizViewModel(_scoringService, _audioManager, _appUserService, _learningService);
    }

    [Fact]
    public async Task LoadQuestionAsync_ShouldNavigateBack_WhenNetworkIsNotAvailable()
    {
        // Arrange
        _viewModel.NetworkAvailable = false;

        // Act
        await _viewModel.LoadQuestionAsync();

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be("..");
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
                new() { Id = 1, Word = "Test1" },
                new() { Id = 2, Word = "Test2" }
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

        for (var i = 1; i <= 5; i++) items.Add(new CollectionItem { Id = i, Word = $"Word{i}" });

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

    [Fact]
    public async Task ImportCollectionAsync_ShouldFilterDuplicates_AndPrioritizeAudio()
    {
        // Arrange
        var itemWithAudio = new CollectionItem { Id = 1, Word = "fork", Audio = "http://audio" };
        var itemWithoutAudio = new CollectionItem { Id = 2, Word = "Fork", Audio = "" };
        var itemOther = new CollectionItem { Id = 3, Word = "Spoon" };

        var collection = new WordCollection
        {
            Items = new List<CollectionItem> { itemWithoutAudio, itemWithAudio, itemOther }
        };
        _viewModel.SelectedCollection = collection;
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(10);

        // Act
        await _viewModel.ImportCollectionAsync();


        var field = typeof(AudioQuizViewModel).GetField("_allWords", BindingFlags.NonPublic | BindingFlags.Instance);
        var allWords = (List<CollectionItem>)field.GetValue(_viewModel);

        // Assert
        allWords.Should().HaveCount(2);
        allWords.Should().Contain(i => i.Word.Equals("fork", StringComparison.OrdinalIgnoreCase));
        allWords.Should().Contain(i => i.Word == "Spoon");


        var forkItem = allWords.First(i => i.Word.Equals("fork", StringComparison.OrdinalIgnoreCase));
        forkItem.Audio.Should().Be("http://audio");
    }


    private class TestableAudioQuizViewModel : AudioQuizViewModel
    {
        public TestableAudioQuizViewModel(IScoringService scoringService, IAudioManager audioManager,
            IAppUserService appUserService, ILearningService learningService)
            : base(scoringService, audioManager, appUserService, learningService)
        {
        }

        public bool NetworkAvailable { get; set; } = true;
        public string LastNavigatedRoute { get; private set; } = string.Empty;

        protected override bool IsNetworkConnected()
        {
            return NetworkAvailable;
        }

        protected override Task ShowAlert(string title, string message, string cancel)
        {
            return Task.CompletedTask;
        }

        protected override Task GoToAsync(string route)
        {
            LastNavigatedRoute = route;
            return Task.CompletedTask;
        }

        protected override string GetCacheDirectory()
        {
            return Path.GetTempPath();
        }
    }
}