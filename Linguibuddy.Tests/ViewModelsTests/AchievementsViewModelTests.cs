using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using LocalizationResourceManager.Maui;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class AchievementsViewModelTests
{
    private readonly IAchievementService _achievementService;
    private readonly IAchievementRepository _achievementRepository;
    private readonly ILocalizationResourceManager _localizationResourceManager;
    private readonly AchievementsViewModel _viewModel;

    public AchievementsViewModelTests()
    {
        _achievementService = A.Fake<IAchievementService>();
        _achievementRepository = A.Fake<IAchievementRepository>();
        _localizationResourceManager = A.Fake<ILocalizationResourceManager>();
        _viewModel = new AchievementsViewModel(_achievementService, _achievementRepository, _localizationResourceManager);
    }

    [Fact]
    public async Task LoadAchievementsAsync_ShouldCallServiceAndRepository()
    {
        // Arrange
        var userAchievements = new List<UserAchievement>();
        A.CallTo(() => _achievementRepository.GetUserAchievementsAsNoTrackingAsync()).Returns(userAchievements);

        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        A.CallTo(() => _achievementService.CheckAchievementsAsync()).MustHaveHappenedOnceExactly();
        A.CallTo(() => _achievementRepository.GetUserAchievementsAsNoTrackingAsync()).MustHaveHappenedOnceExactly();
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAchievementsAsync_ShouldFilterAchievementsCorrectly()
    {
        // Arrange
        var achievements = new List<UserAchievement>
        {
            // Unlocked
            new() { 
                IsUnlocked = true, 
                Achievement = new Achievement { UnlockCondition = AchievementUnlockType.TotalPoints, Name = "A1" } 
            },
            // Locked but last was unlocked (should be visible)
            new() { 
                IsUnlocked = false, 
                Achievement = new Achievement { UnlockCondition = AchievementUnlockType.TotalPoints, Name = "A2" } 
            },
            // Locked and previous was locked (should be skipped)
            new() { 
                IsUnlocked = false, 
                Achievement = new Achievement { UnlockCondition = AchievementUnlockType.TotalPoints, Name = "A3" } 
            },
            // Locked but NEW type (should be visible)
            new() { 
                IsUnlocked = false, 
                Achievement = new Achievement { UnlockCondition = AchievementUnlockType.LearningStreak, Name = "B1" } 
            }
        };

        A.CallTo(() => _achievementRepository.GetUserAchievementsAsNoTrackingAsync()).Returns(achievements);

        // Act
        await _viewModel.LoadAchievementsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Achievements.Should().HaveCount(3);
        _viewModel.Achievements.Should().Contain(a => a.Achievement.Name == "A1");
        _viewModel.Achievements.Should().Contain(a => a.Achievement.Name == "A2");
        _viewModel.Achievements.Should().Contain(a => a.Achievement.Name == "B1");
        _viewModel.Achievements.Should().NotContain(a => a.Achievement.Name == "A3");
    }
}