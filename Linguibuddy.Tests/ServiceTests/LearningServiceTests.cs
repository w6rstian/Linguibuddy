using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.Tests.ServiceTests;

public class LearningServiceTests
{
    private readonly IUserLearningDayRepository _repo;
    private readonly IAppUserRepository _appUserRepo;
    private readonly IAppUserService _appUserService;
    private readonly IAuthService _authService;
    private readonly LearningService _sut;
    private readonly string _userId = "user123";

    public LearningServiceTests()
    {
        _repo = A.Fake<IUserLearningDayRepository>();
        _appUserRepo = A.Fake<IAppUserRepository>();
        _appUserService = A.Fake<IAppUserService>();
        _authService = A.Fake<IAuthService>();

        A.CallTo(() => _authService.CurrentUserId).Returns(_userId);

        _sut = new LearningService(_repo, _appUserRepo, _appUserService, _authService);
    }

    [Fact]
    public async Task MarkLearnedTodayAsync_ShouldAddEntry_WhenNotExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(user);
        A.CallTo(() => _repo.ExistsAsync(_userId, DateTime.Today)).Returns(false);

        // Act
        await _sut.MarkLearnedTodayAsync();

        // Assert
        A.CallTo(() => _repo.AddAsync(A<UserLearningDay>.That.Matches(d => 
            d.AppUserId == _userId && 
            d.Date == DateTime.Today && 
            d.Learned == true)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task MarkLearnedTodayAsync_ShouldNotAddEntry_WhenAlreadyExists()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(user);
        A.CallTo(() => _repo.ExistsAsync(_userId, DateTime.Today)).Returns(true);

        // Act
        await _sut.MarkLearnedTodayAsync();

        // Assert
        A.CallTo(() => _repo.AddAsync(A<UserLearningDay>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task MarkLearnedTodayAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.MarkLearnedTodayAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
        A.CallTo(() => _repo.AddAsync(A<UserLearningDay>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetCurrentStreakAsync_ShouldReturnZero_WhenNoLearningDays()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(user);
        A.CallTo(() => _repo.GetLearningDatesAsync(_userId)).Returns(new List<DateTime>());

        // Act
        var result = await _sut.GetCurrentStreakAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetCurrentStreakAsync_ShouldReturn1_WhenLearnedTodayOnly()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        var dates = new List<DateTime> { DateTime.Today };
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(user);
        A.CallTo(() => _repo.GetLearningDatesAsync(_userId)).Returns(dates);

        // Act
        var result = await _sut.GetCurrentStreakAsync();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentStreakAsync_ShouldReturn3_WhenLearnedLast3Days()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        // Dates should be returned in descending order as per service assumption (it checks sequentially backwards from today)
        // Service logic: expected = Today, if match streak++, expected = expected - 1 day.
        var dates = new List<DateTime> 
        { 
            DateTime.Today, 
            DateTime.Today.AddDays(-1), 
            DateTime.Today.AddDays(-2) 
        };
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(user);
        A.CallTo(() => _repo.GetLearningDatesAsync(_userId)).Returns(dates);

        // Act
        var result = await _sut.GetCurrentStreakAsync();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetCurrentStreakAsync_ShouldBreakStreak_WhenDaySkipped()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        var dates = new List<DateTime> 
        { 
            DateTime.Today, 
            DateTime.Today.AddDays(-2) // Skipped yesterday
        };
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(user);
        A.CallTo(() => _repo.GetLearningDatesAsync(_userId)).Returns(dates);

        // Act
        var result = await _sut.GetCurrentStreakAsync();

        // Assert
        result.Should().Be(1); // Only today counts
    }

    [Fact]
    public async Task GetCurrentStreakAsync_ShouldUpdateBestStreak_WhenCurrentIsHigher()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        var dates = new List<DateTime> 
        { 
            DateTime.Today, 
            DateTime.Today.AddDays(-1) 
        };
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(user);
        A.CallTo(() => _repo.GetLearningDatesAsync(_userId)).Returns(dates);
        A.CallTo(() => _appUserService.GetUserBestStreakAsync()).Returns(1); // Previous best was 1

        // Act
        await _sut.GetCurrentStreakAsync();

        // Assert
        A.CallTo(() => _appUserService.SetBestLearningStreakAsync(2)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetCurrentStreakAsync_ShouldNotUpdateBestStreak_WhenCurrentIsLower()
    {
        // Arrange
        var user = new AppUser { Id = _userId };
        var dates = new List<DateTime> { DateTime.Today };
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(user);
        A.CallTo(() => _repo.GetLearningDatesAsync(_userId)).Returns(dates);
        A.CallTo(() => _appUserService.GetUserBestStreakAsync()).Returns(5); // Best is 5

        // Act
        await _sut.GetCurrentStreakAsync();

        // Assert
        A.CallTo(() => _appUserService.SetBestLearningStreakAsync(A<int>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetCurrentStreakAsync_ShouldThrowException_WhenUserNotFound()
    {
        // Arrange
        A.CallTo(() => _appUserRepo.GetByIdAsync(_userId)).Returns(Task.FromResult<AppUser?>(null));

        // Act
        var act = async () => await _sut.GetCurrentStreakAsync();

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("User not found");
    }
}
