using System.Globalization;
using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;

namespace Linguibuddy.Tests.ViewModelsTests;

public class LeaderboardViewModelTests
{
    private readonly IAppUserService _appUserService;
    private readonly TestableLeaderboardViewModel _viewModel;

    public LeaderboardViewModelTests()
    {
        _appUserService = A.Fake<IAppUserService>();
        _viewModel = new TestableLeaderboardViewModel(_appUserService);
    }

    [Fact]
    public async Task LoadLeaderboardAsync_ShouldPopulateItems_WhenServiceReturnsUsers()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("pl-PL");

        try
        {
            var users = new List<AppUser>
            {
                new() { Id = "1", UserName = "Alice", Points = 100 },
                new() { Id = "2", UserName = "Bob", Points = 80 },
                new() { Id = "3", Points = 50 } // No username
            };
            A.CallTo(() => _appUserService.GetLeaderboardAsync(50)).Returns(users);

            // Act
            await _viewModel.LoadLeaderboardCommand.ExecuteAsync(null);

            // Assert
            _viewModel.LeaderboardItems.Should().HaveCount(3);

            var first = _viewModel.LeaderboardItems[0];
            first.Rank.Should().Be(1);
            first.UserName.Should().Be("Alice");
            first.Points.Should().Be(100);
            first.IsTop3.Should().BeTrue();

            var third = _viewModel.LeaderboardItems[2];
            third.Rank.Should().Be(3);
            third.UserName.Should().Be("Anonim"); // Default name check
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task LoadLeaderboardAsync_ShouldShowError_WhenServiceFails()
    {
        // Arrange
        A.CallTo(() => _appUserService.GetLeaderboardAsync(50)).Throws(new Exception("API Error"));

        // Act
        await _viewModel.LoadLeaderboardCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LeaderboardItems.Should().BeEmpty();
        _viewModel.LastAlertMessage.Should().NotBeNullOrEmpty();
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadLeaderboardAsync_ShouldDoNothing_WhenAlreadyLoading()
    {
        // Arrange
        _viewModel.IsLoading = true;

        // Act
        await _viewModel.LoadLeaderboardCommand.ExecuteAsync(null);

        // Assert
        A.CallTo(() => _appUserService.GetLeaderboardAsync(A<int>.Ignored)).MustNotHaveHappened();
    }

    private class TestableLeaderboardViewModel : LeaderboardViewModel
    {
        public TestableLeaderboardViewModel(IAppUserService appUserService) : base(appUserService)
        {
        }

        public string? LastAlertMessage { get; private set; }

        protected override Task ShowAlertAsync(string title, string message, string cancel)
        {
            LastAlertMessage = message;
            return Task.CompletedTask;
        }
    }
}