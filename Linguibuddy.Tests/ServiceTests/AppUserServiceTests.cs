using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.Tests.ServiceTests;

public class AppUserServiceTests
{
    private readonly IAppUserRepository _repo;
    private readonly IAuthService _auth;
    private readonly AppUserService _sut;
    private readonly string _userId = "user123";

    public AppUserServiceTests()
    {
        _repo = A.Fake<IAppUserRepository>();
        _auth = A.Fake<IAuthService>();
        
        A.CallTo(() => _auth.CurrentUserId).Returns(_userId);

        _sut = new AppUserService(_repo, _auth);
    }

    [Fact]
    public async Task AddUserPointsAsync_ShouldAddPoints_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, Points = 100 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        await _sut.AddUserPointsAsync(50);

        // Assert
        user.Points.Should().Be(150);
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserDifficultyAsync_ShouldReturnDefault_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var result = await _sut.GetUserDifficultyAsync();

        // Assert
        result.Should().Be(DifficultyLevel.A1);
    }

    [Fact]
    public async Task SetUserDifficultyAsync_ShouldUpdateDifficulty_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, DifficultyLevel = DifficultyLevel.A1 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        await _sut.SetUserDifficultyAsync(DifficultyLevel.B2);

        // Assert
        user.DifficultyLevel.Should().Be(DifficultyLevel.B2);
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserPointsAsync_ShouldReturnPoints_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, Points = 500 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        var result = await _sut.GetUserPointsAsync();

        // Assert
        result.Should().Be(500);
    }

    [Fact]
    public async Task GetUserLessonLengthAsync_ShouldReturnDefault_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var result = await _sut.GetUserLessonLengthAsync();

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public async Task GetUserLessonLengthAsync_ShouldReturnLength_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, LessonLength = 20 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        var result = await _sut.GetUserLessonLengthAsync();

        // Assert
        result.Should().Be(20);
    }

    [Fact]
    public async Task SetUserLessonLengthAsync_ShouldUpdateLength_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, LessonLength = 10 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        await _sut.SetUserLessonLengthAsync(15);

        // Assert
        user.LessonLength.Should().Be(15);
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetUserBestStreakAsync_ShouldReturnStreak_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, BestLearningStreak = 5 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        var result = await _sut.GetUserBestStreakAsync();

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task SetBestLearningStreakAsync_ShouldUpdateStreak_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, BestLearningStreak = 3 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        await _sut.SetBestLearningStreakAsync(10);

        // Assert
        user.BestLearningStreak.Should().Be(10);
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }
}
