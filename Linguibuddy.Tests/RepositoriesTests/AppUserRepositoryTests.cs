using FluentAssertions;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Tests.RepositoriesTests;

public class AppUserRepositoryTests : IDisposable
{
    private readonly DataContext _context;
    private readonly AppUserRepository _sut;

    public AppUserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new DataContext(options);
        _sut = new AppUserRepository(_context);
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldReturnUsersOrderedByPointsDescending()
    {
        // Arrange
        var user1 = new AppUser { Id = "u1", Points = 10, UserName = "Low" };
        var user2 = new AppUser { Id = "u2", Points = 50, UserName = "High" };
        var user3 = new AppUser { Id = "u3", Points = 30, UserName = "Mid" };
        
        _context.AppUsers.AddRange(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetTopUsersAsync(3);

        // Assert
        result.Should().HaveCount(3);
        result[0].Id.Should().Be("u2");
        result[1].Id.Should().Be("u3");
        result[2].Id.Should().Be("u1");
    }

    [Fact]
    public async Task GetTopUsersAsync_ShouldLimitResultCount()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _context.AppUsers.Add(new AppUser { Id = $"u{i}", Points = i });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetTopUsersAsync(5);

        // Assert
        result.Should().HaveCount(5);
        result.First().Points.Should().Be(9);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var userId = "user123";
        var user = new AppUser { Id = userId, Points = 100 };
        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Points.Should().Be(100);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _sut.GetByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserAndSaveToDb()
    {
        // Arrange
        var user = new AppUser { Id = "newUser", Points = 50 };

        // Act
        await _sut.AddAsync(user);

        // Assert
        var savedUser = await _context.AppUsers.FindAsync("newUser");
        savedUser.Should().NotBeNull();
        savedUser!.Points.Should().Be(50);
    }

    [Fact]
    public async Task Update_ShouldMarkEntityAsModified()
    {
        // Arrange
        var user = new AppUser { Id = "user1", Points = 10 };
        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        user.Points = 20;
        _sut.Update(user);

        // Assert
        _context.Entry(user).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistChangesToDb()
    {
        // Arrange
        var user = new AppUser { Id = "user1", Points = 10 };
        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        user.Points = 20;
        _sut.Update(user);

        // Act
        await _sut.SaveChangesAsync();

        // Assert
        _context.ChangeTracker.Clear();
        var savedUser = await _context.AppUsers.FindAsync("user1");
        savedUser!.Points.Should().Be(20);
    }
}