using FluentAssertions;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.Tests.ServiceTests;

public class SpacedRepetitionServiceTests
{
    private readonly SpacedRepetitionService _sut;

    public SpacedRepetitionServiceTests()
    {
        _sut = new SpacedRepetitionService();
    }

    [Fact]
    public void ProcessResult_ShouldIncreaseInterval_WhenGradeIsPassing()
    {
        // Arrange
        var card = new Flashcard
        {
            Repetitions = 2,
            Interval = 6,
            EaseFactor = 2.5
        };
        var initialInterval = card.Interval;
        var grade = 4; // Passing

        // Act
        _sut.ProcessResult(card, grade);

        // Assert
        card.Interval.Should().BeGreaterThan(initialInterval);
        card.Repetitions.Should().Be(3);
        card.NextReviewDate.Date.Should().Be(DateTime.UtcNow.AddDays(card.Interval).Date);
    }

    [Fact]
    public void ProcessResult_ShouldResetRepetitions_WhenGradeIsFailing()
    {
        // Arrange
        var card = new Flashcard
        {
            Repetitions = 5,
            Interval = 10,
            EaseFactor = 2.5
        };
        var grade = 2; // Failing (< 3)

        // Act
        _sut.ProcessResult(card, grade);

        // Assert
        card.Repetitions.Should().Be(0);
        card.Interval.Should().Be(1);
    }

    [Fact]
    public void ProcessResult_ShouldNotDecreaseEaseFactorBelow13()
    {
        // Arrange
        var card = new Flashcard
        {
            Repetitions = 1,
            Interval = 1,
            EaseFactor = 1.3
        };
        var grade = 0; // Worst grade

        // Act
        _sut.ProcessResult(card, grade);

        // Assert
        card.EaseFactor.Should().Be(1.3); // Minimum cap
    }

    [Fact]
    public void ProcessResult_ShouldSetIntervalTo1_OnFirstRepetition()
    {
        // Arrange
        var card = new Flashcard
        {
            Repetitions = 0,
            Interval = 0,
            EaseFactor = 2.5
        };
        var grade = 4;

        // Act
        _sut.ProcessResult(card, grade);

        // Assert
        card.Interval.Should().Be(1);
        card.Repetitions.Should().Be(1);
    }

    [Fact]
    public void ProcessResult_ShouldSetIntervalTo6_OnSecondRepetition()
    {
        // Arrange
        var card = new Flashcard
        {
            Repetitions = 1,
            Interval = 1,
            EaseFactor = 2.5
        };
        var grade = 4;

        // Act
        _sut.ProcessResult(card, grade);

        // Assert
        card.Interval.Should().Be(6);
        card.Repetitions.Should().Be(2);
    }
}
