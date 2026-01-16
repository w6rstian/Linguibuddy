using FluentAssertions;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Tests.RepositoriesTests;

public class UserLearningDayRepositoryTests : IDisposable
{
    private readonly DataContext _context;
    private readonly UserLearningDayRepository _sut;

    public UserLearningDayRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new DataContext(options);
        _sut = new UserLearningDayRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenRecordExists()
    {
        // Arrange
        var userId = "user1";
        var date = DateTime.Today;
        _context.UserLearningDays.Add(new UserLearningDay { AppUserId = userId, Date = date });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ExistsAsync(userId, date);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenRecordDoesNotExist()
    {
        // Arrange
        var userId = "user1";
        var date = DateTime.Today;

        // Act
        var result = await _sut.ExistsAsync(userId, date);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ShouldAddRecordAndSaveToDb()
    {
        // Arrange
        var day = new UserLearningDay { AppUserId = "user1", Date = DateTime.Today };

        // Act
        await _sut.AddAsync(day);

        // Assert
        var savedDay = await _context.UserLearningDays.FirstOrDefaultAsync(d => d.AppUserId == "user1");
        savedDay.Should().NotBeNull();
        savedDay!.Date.Should().Be(DateTime.Today);
    }

    [Fact]
    public async Task GetLearningDatesAsync_ShouldReturnDatesForSpecificUser()
    {
        // Arrange
        var userId1 = "user1";
        var userId2 = "user2";
        var date1 = DateTime.Today;
        var date2 = DateTime.Today.AddDays(-1);

        _context.UserLearningDays.AddRange(
            new UserLearningDay { AppUserId = userId1, Date = date1 },
            new UserLearningDay { AppUserId = userId1, Date = date2 },
            new UserLearningDay { AppUserId = userId2, Date = date1 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetLearningDatesAsync(userId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(date1);
        result.Should().Contain(date2);
        result.Should().NotContain(d => d == DateTime.Today.AddDays(1));
    }

    [Fact]
    public async Task GetLearningDatesAsync_ShouldReturnDatesOrderedDescending()
    {
        // Arrange
        var userId = "user1";
        var date1 = DateTime.Today.AddDays(-2);
        var date2 = DateTime.Today;
        var date3 = DateTime.Today.AddDays(-1);

        _context.UserLearningDays.AddRange(
            new UserLearningDay { AppUserId = userId, Date = date1 },
            new UserLearningDay { AppUserId = userId, Date = date2 },
            new UserLearningDay { AppUserId = userId, Date = date3 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetLearningDatesAsync(userId);

        // Assert
        result.Should().BeInDescendingOrder();
        result.First().Should().Be(date2);
        result.Last().Should().Be(date1);
    }
}