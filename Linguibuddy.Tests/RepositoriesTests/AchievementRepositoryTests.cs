using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Tests.RepositoriesTests;

public class AchievementRepositoryTests : IDisposable
{
    private readonly IAuthService _authService;
    private readonly DataContext _context;
    private readonly AchievementRepository _sut;
    private readonly string _userId = "user123";

    public AchievementRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new DataContext(options);
        _authService = A.Fake<IAuthService>();
        A.CallTo(() => _authService.CurrentUserId).Returns(_userId);

        _sut = new AchievementRepository(_context, _authService);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetUserAchievementsAsync_ShouldReturnAchievementsForCurrentUser()
    {
        // Arrange
        var achievement1 = new Achievement { Id = 1, Name = "A1" };
        var achievement2 = new Achievement { Id = 2, Name = "A2" };
        _context.Achievements.AddRange(achievement1, achievement2);

        var userAchievement1 = new UserAchievement
            { Id = 1, AppUserId = _userId, AchievementId = 1, Achievement = achievement1 };
        var userAchievement2 = new UserAchievement
            { Id = 2, AppUserId = "otherUser", AchievementId = 2, Achievement = achievement2 };
        _context.UserAchievements.AddRange(userAchievement1, userAchievement2);

        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserAchievementsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().AppUserId.Should().Be(_userId);
        result.First().Achievement.Should().NotBeNull();
        result.First().Achievement.Name.Should().Be("A1");
    }

    [Fact]
    public async Task GetUserAchievementsAsNoTrackingAsync_ShouldReturnAchievementsForCurrentUser_AsNoTracking()
    {
        // Arrange
        var achievement1 = new Achievement { Id = 1, Name = "A1" };
        _context.Achievements.Add(achievement1);

        var userAchievement1 = new UserAchievement
            { Id = 1, AppUserId = _userId, AchievementId = 1, Achievement = achievement1 };
        _context.UserAchievements.Add(userAchievement1);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetUserAchievementsAsNoTrackingAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().AppUserId.Should().Be(_userId);

        var entry = _context.ChangeTracker.Entries<UserAchievement>()
            .FirstOrDefault(e => e.Entity.Id == result.First().Id);

        entry.Should().BeNull("because the query was executed with AsNoTracking");
    }

    [Fact]
    public async Task GetUnlockedAchievementsCountAsync_ShouldReturnCountOfUnlockedAchievementsForUser()
    {
        // Arrange
        var ua1 = new UserAchievement { Id = 1, AppUserId = _userId, IsUnlocked = true };
        var ua2 = new UserAchievement { Id = 2, AppUserId = _userId, IsUnlocked = false };
        var ua3 = new UserAchievement { Id = 3, AppUserId = "otherUser", IsUnlocked = true };
        var ua4 = new UserAchievement { Id = 4, AppUserId = _userId, IsUnlocked = true };

        _context.UserAchievements.AddRange(ua1, ua2, ua3, ua4);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUnlockedAchievementsCountAsync();

        // Assert
        result.Should().Be(2); // ua1 and ua4
    }
}