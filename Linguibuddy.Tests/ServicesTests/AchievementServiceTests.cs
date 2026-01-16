using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Data;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Tests.ServicesTests;

public class AchievementServiceTests : IDisposable
{
    private readonly IAppUserService _appUserService;
    private readonly IAuthService _auth;
    private readonly DataContext _db;
    private readonly IAchievementRepository _repo;
    private readonly AchievementService _sut;
    private readonly string _userId = "user123";

    public AchievementServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new DataContext(options);
        _auth = A.Fake<IAuthService>();
        _appUserService = A.Fake<IAppUserService>();
        _repo = A.Fake<IAchievementRepository>();

        A.CallTo(() => _auth.CurrentUserId).Returns(_userId);

        _sut = new AchievementService(_db, _auth, _appUserService, _repo);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockPointsAchievement_WhenThresholdReached()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = 1,
            UnlockCondition = AchievementUnlockType.TotalPoints,
            UnlockTargetValue = 100
        };
        var userAchievement = new UserAchievement
        {
            Id = 101,
            AppUserId = _userId,
            AchievementId = 1,
            Achievement = achievement,
            IsUnlocked = false
        };

        _db.UserAchievements.Add(userAchievement);
        await _db.SaveChangesAsync();

        A.CallTo(() => _repo.GetUserAchievementsAsync()).Returns(new List<UserAchievement> { userAchievement });
        A.CallTo(() => _appUserService.GetUserPointsAsync()).Returns(150);
        A.CallTo(() => _appUserService.GetUserBestStreakAsync()).Returns(0);

        // Act
        await _sut.CheckAchievementsAsync();

        // Assert
        var result = await _db.UserAchievements.FirstAsync(ua => ua.Id == 101);
        result.IsUnlocked.Should().BeTrue();
        result.UnlockDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldNotUnlockPointsAchievement_WhenThresholdNotReached()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = 1,
            UnlockCondition = AchievementUnlockType.TotalPoints,
            UnlockTargetValue = 100
        };
        var userAchievement = new UserAchievement
        {
            Id = 101,
            AppUserId = _userId,
            AchievementId = 1,
            Achievement = achievement,
            IsUnlocked = false
        };

        _db.UserAchievements.Add(userAchievement);
        await _db.SaveChangesAsync();

        A.CallTo(() => _repo.GetUserAchievementsAsync()).Returns(new List<UserAchievement> { userAchievement });
        A.CallTo(() => _appUserService.GetUserPointsAsync()).Returns(50);

        // Act
        await _sut.CheckAchievementsAsync();

        // Assert
        var result = await _db.UserAchievements.FirstAsync(ua => ua.Id == 101);
        result.IsUnlocked.Should().BeFalse();
        result.UnlockDate.Should().BeNull();
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldUnlockStreakAchievement_WhenThresholdReached()
    {
        // Arrange
        var achievement = new Achievement
        {
            Id = 2,
            UnlockCondition = AchievementUnlockType.LearningStreak,
            UnlockTargetValue = 7
        };
        var userAchievement = new UserAchievement
        {
            Id = 102,
            AppUserId = _userId,
            AchievementId = 2,
            Achievement = achievement,
            IsUnlocked = false
        };

        _db.UserAchievements.Add(userAchievement);
        await _db.SaveChangesAsync();

        A.CallTo(() => _repo.GetUserAchievementsAsync()).Returns(new List<UserAchievement> { userAchievement });
        A.CallTo(() => _appUserService.GetUserPointsAsync()).Returns(0);
        A.CallTo(() => _appUserService.GetUserBestStreakAsync()).Returns(10);

        // Act
        await _sut.CheckAchievementsAsync();

        // Assert
        var result = await _db.UserAchievements.FirstAsync(ua => ua.Id == 102);
        result.IsUnlocked.Should().BeTrue();
        result.UnlockDate.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldDoNothing_WhenNoAchievementsFound()
    {
        // Arrange
        A.CallTo(() => _repo.GetUserAchievementsAsync()).Returns(new List<UserAchievement>());

        // Act
        await _sut.CheckAchievementsAsync();

        // Assert
        A.CallTo(() => _appUserService.GetUserPointsAsync()).MustNotHaveHappened();
    }

    [Fact]
    public async Task CheckAchievementsAsync_ShouldNotReUnlock_WhenAlreadyUnlocked()
    {
        // Arrange
        var unlockDate = DateTime.Today.AddDays(-5);
        var achievement = new Achievement
        {
            Id = 1,
            UnlockCondition = AchievementUnlockType.TotalPoints,
            UnlockTargetValue = 100
        };
        var userAchievement = new UserAchievement
        {
            Id = 101,
            AppUserId = _userId,
            AchievementId = 1,
            Achievement = achievement,
            IsUnlocked = true,
            UnlockDate = unlockDate
        };

        _db.UserAchievements.Add(userAchievement);
        await _db.SaveChangesAsync();

        A.CallTo(() => _repo.GetUserAchievementsAsync()).Returns(new List<UserAchievement> { userAchievement });
        A.CallTo(() => _appUserService.GetUserPointsAsync()).Returns(150);

        // Act
        await _sut.CheckAchievementsAsync();

        // Assert
        var result = await _db.UserAchievements.FirstAsync(ua => ua.Id == 101);
        result.IsUnlocked.Should().BeTrue();
        result.UnlockDate.Should().Be(unlockDate);
    }
}