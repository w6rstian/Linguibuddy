using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.Tests.ServicesTests;

public class ScoringServiceTests
{
    private readonly IAppUserService _appUserService;
    private readonly ICollectionService _collectionService;
    private readonly ScoringService _sut;

    public ScoringServiceTests()
    {
        _collectionService = A.Fake<ICollectionService>();
        _appUserService = A.Fake<IAppUserService>();
        _sut = new ScoringService(_collectionService, _appUserService);
    }

    [Theory]
    [InlineData(GameType.AudioQuiz, DifficultyLevel.A1, 10)]
    [InlineData(GameType.ImageQuiz, DifficultyLevel.A1, 10)]
    [InlineData(GameType.Hangman, DifficultyLevel.A1, 50)]
    public void CalculatePoints_ShouldReturnBasePoints_ForStandardGames(GameType gameType, DifficultyLevel difficulty,
        int expectedPoints)
    {
        // Act
        var result = _sut.CalculatePoints(gameType, difficulty);

        // Assert
        result.Should().Be(expectedPoints);
    }

    [Theory]
    [InlineData(GameType.SentenceQuiz, DifficultyLevel.A1, 20)] // No bonus
    [InlineData(GameType.SentenceQuiz, DifficultyLevel.A2, 22)] // 20 + 10% = 22
    [InlineData(GameType.SentenceQuiz, DifficultyLevel.B1, 26)] // 20 + 30% = 26
    [InlineData(GameType.SentenceQuiz, DifficultyLevel.B2, 30)] // 20 + 50% = 30
    [InlineData(GameType.SentenceQuiz, DifficultyLevel.C1, 36)] // 20 + 80% = 36
    [InlineData(GameType.SentenceQuiz, DifficultyLevel.C2, 40)] // 20 + 100% = 40
    public void CalculatePoints_ShouldApplyDifficultyBonus_ForSentenceQuiz(GameType gameType,
        DifficultyLevel difficulty, int expectedPoints)
    {
        // Act
        var result = _sut.CalculatePoints(gameType, difficulty);

        // Assert
        result.Should().Be(expectedPoints);
    }

    [Theory]
    [InlineData(GameType.SpeakingQuiz, DifficultyLevel.A1, 20)] // No bonus
    [InlineData(GameType.SpeakingQuiz, DifficultyLevel.B2, 30)] // 20 + 50% = 30
    public void CalculatePoints_ShouldApplyDifficultyBonus_ForSpeakingQuiz(GameType gameType,
        DifficultyLevel difficulty, int expectedPoints)
    {
        // Act
        var result = _sut.CalculatePoints(gameType, difficulty);

        // Assert
        result.Should().Be(expectedPoints);
    }

    [Fact]
    public async Task SaveResultsAsync_ShouldUpdateCollectionAndAddPoints_WhenPointsEarned()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };
        var gameType = GameType.AudioQuiz;
        var correctAnswers = 8;
        var totalQuestions = 10;
        var totalPoints = 50;

        // Act
        await _sut.SaveResultsAsync(collection, gameType, correctAnswers, totalQuestions, totalPoints);

        // Assert
        collection.AudioLastScore.Should().Be(0.8);
        collection.AudioBestScore.Should().Be(0.8);
        collection.AudioLastPlayed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        collection.RequiresAiAnalysis.Should().BeTrue();

        A.CallTo(() => _collectionService.UpdateCollectionAsync(collection)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _appUserService.AddUserPointsAsync(totalPoints)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _appUserService.MarkAiAnalysisRequiredAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SaveResultsAsync_ShouldNotAddPoints_WhenZeroPointsEarned()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };
        var gameType = GameType.ImageQuiz;
        var correctAnswers = 0;
        var totalQuestions = 10;
        var totalPoints = 0;

        // Act
        await _sut.SaveResultsAsync(collection, gameType, correctAnswers, totalQuestions, totalPoints);

        // Assert
        collection.ImageLastScore.Should().Be(0.0);

        A.CallTo(() => _collectionService.UpdateCollectionAsync(collection)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _appUserService.AddUserPointsAsync(A<int>._)).MustNotHaveHappened();
        A.CallTo(() => _appUserService.MarkAiAnalysisRequiredAsync()).MustNotHaveHappened();
    }

    [Fact]
    public async Task SaveResultsAsync_ShouldNotOverwriteBestScore_WhenCurrentScoreIsLower()
    {
        // Arrange
        var collection = new WordCollection
        {
            Id = 1,
            HangmanBestScore = 0.9
        };
        var gameType = GameType.Hangman;
        var correctAnswers = 5;
        var totalQuestions = 10; // Ratio 0.5
        var totalPoints = 10;

        // Act
        await _sut.SaveResultsAsync(collection, gameType, correctAnswers, totalQuestions, totalPoints);

        // Assert
        collection.HangmanLastScore.Should().Be(0.5);
        collection.HangmanBestScore.Should().Be(0.9);

        A.CallTo(() => _collectionService.UpdateCollectionAsync(collection)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SaveResultsAsync_ShouldDoNothing_WhenTotalQuestionsIsZero()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };

        // Act
        await _sut.SaveResultsAsync(collection, GameType.AudioQuiz, 0, 0, 10);

        // Assert
        A.CallTo(() => _collectionService.UpdateCollectionAsync(A<WordCollection>._)).MustNotHaveHappened();
        A.CallTo(() => _appUserService.AddUserPointsAsync(A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SaveResultsAsync_ShouldUpdateStats_ForSentenceQuiz()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };

        // Act
        await _sut.SaveResultsAsync(collection, GameType.SentenceQuiz, 10, 10, 100);

        // Assert
        collection.SentenceLastScore.Should().Be(1.0);
        collection.SentenceBestScore.Should().Be(1.0);
        collection.SentenceLastPlayed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SaveResultsAsync_ShouldUpdateStats_ForSpeakingQuiz()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };

        // Act
        await _sut.SaveResultsAsync(collection, GameType.SpeakingQuiz, 5, 10, 50);

        // Assert
        collection.SpeakingLastScore.Should().Be(0.5);
        collection.SpeakingBestScore.Should().Be(0.5);
        collection.SpeakingLastPlayed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}