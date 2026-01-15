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
    public async Task AddUserPointsAsync_ShouldAddPointsAndSave_WhenUserExists()
    {
        // Arrange
        var initialPoints = 100;
        var pointsToAdd = 50;
        var user = new AppUser { Id = _userId, Points = initialPoints };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        await _sut.AddUserPointsAsync(pointsToAdd);

        // Assert
        user.Points.Should().Be(initialPoints + pointsToAdd);
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddUserPointsAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.AddUserPointsAsync(50);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        A.CallTo(() => _repo.SaveChangesAsync()).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetUserDifficultyAsync_ShouldReturnDifficulty_WhenUserExists()
    {
        // Arrange
        var expectedDifficulty = DifficultyLevel.B2;
        var user = new AppUser { Id = _userId, DifficultyLevel = expectedDifficulty };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        var result = await _sut.GetUserDifficultyAsync();

        // Assert
        result.Should().Be(expectedDifficulty);
    }

    [Fact]
    public async Task GetUserDifficultyAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.GetUserDifficultyAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
    }

    [Fact]
    public async Task SetUserDifficultyAsync_ShouldUpdateDifficultyAndSave_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, DifficultyLevel = DifficultyLevel.A1 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);
        var newDifficulty = DifficultyLevel.C1;

        // Act
        await _sut.SetUserDifficultyAsync(newDifficulty);

        // Assert
        user.DifficultyLevel.Should().Be(newDifficulty);
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SetUserDifficultyAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.SetUserDifficultyAsync(DifficultyLevel.B1);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        A.CallTo(() => _repo.SaveChangesAsync()).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetUserPointsAsync_ShouldReturnPoints_WhenUserExists()
    {
        // Arrange
        var expectedPoints = 500;
        var user = new AppUser { Id = _userId, Points = expectedPoints };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        var result = await _sut.GetUserPointsAsync();

        // Assert
        result.Should().Be(expectedPoints);
    }

    [Fact]
    public async Task GetUserPointsAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.GetUserPointsAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
    }

    [Fact]
    public async Task GetUserLessonLengthAsync_ShouldReturnLength_WhenUserExists()
    {
        // Arrange
        var expectedLength = 20;
        var user = new AppUser { Id = _userId, LessonLength = expectedLength };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        var result = await _sut.GetUserLessonLengthAsync();

        // Assert
        result.Should().Be(expectedLength);
    }

    [Fact]
    public async Task GetUserLessonLengthAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.GetUserLessonLengthAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
    }

    [Fact]
    public async Task SetUserLessonLengthAsync_ShouldUpdateLengthAndSave_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, LessonLength = 10 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);
        var newLength = 15;

        // Act
        await _sut.SetUserLessonLengthAsync(newLength);

        // Assert
        user.LessonLength.Should().Be(newLength);
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SetUserLessonLengthAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.SetUserLessonLengthAsync(20);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        A.CallTo(() => _repo.SaveChangesAsync()).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetUserBestStreakAsync_ShouldReturnStreak_WhenUserExists()
    {
        // Arrange
        var expectedStreak = 5;
        var user = new AppUser { Id = _userId, BestLearningStreak = expectedStreak };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        var result = await _sut.GetUserBestStreakAsync();

        // Assert
        result.Should().Be(expectedStreak);
    }

    [Fact]
    public async Task GetUserBestStreakAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.GetUserBestStreakAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
    }

    [Fact]
    public async Task SetBestLearningStreakAsync_ShouldUpdateStreakAndSave_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, BestLearningStreak = 3 };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);
        var newStreak = 10;

        // Act
        await _sut.SetBestLearningStreakAsync(newStreak);

        // Assert
        user.BestLearningStreak.Should().Be(newStreak);
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SetBestLearningStreakAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.SetBestLearningStreakAsync(10);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        A.CallTo(() => _repo.SaveChangesAsync()).MustNotHaveHappened();
    }

    [Fact]
    public async Task MarkAiAnalysisRequiredAsync_ShouldSetFlagAndSave_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId, RequiresAiAnalysis = false };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        await _sut.MarkAiAnalysisRequiredAsync();

        // Assert
        user.RequiresAiAnalysis.Should().BeTrue();
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task MarkAiAnalysisRequiredAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.MarkAiAnalysisRequiredAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        A.CallTo(() => _repo.SaveChangesAsync()).MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdateAppUserAsync_ShouldUpdateRepoAndSave()
    {
        // Arrange
        var user = new AppUser { Id = _userId, Points = 10 };

        // Act
        await _sut.UpdateAppUserAsync(user);

        // Assert
        A.CallTo(() => _repo.Update(user)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _repo.SaveChangesAsync()).MustHaveHappenedOnceExactly();
        
        // Verify internal state update by calling a getter without repo setup
        // The service should use the cached user we just updated
        // Clearing previous repo calls to ensure we don't rely on repo anymore
        A.CallTo(() => _repo.GetByIdAsync(A<string>._)).Returns(Task.FromResult<AppUser?>(null)); 
        
        var points = await _sut.GetUserPointsAsync();
        points.Should().Be(10);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        var result = await _sut.GetCurrentUserAsync();

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.GetCurrentUserAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
    }

    [Fact]
    public async Task Service_ShouldCacheUser_AfterFirstLoad()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        A.CallTo(() => _repo.GetByIdAsync(_userId)).Returns(user);

        // Act
        await _sut.GetUserPointsAsync();
        await _sut.GetUserDifficultyAsync();

        // Assert
        // Should only be called once because the second call uses the cached _appUser
        A.CallTo(() => _repo.GetByIdAsync(_userId)).MustHaveHappenedOnceExactly();
    }
}