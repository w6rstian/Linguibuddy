using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using LocalizationResourceManager.Maui;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class MainViewModelTests
{
    private readonly IServiceProvider _services;
    private readonly IAppUserService _appUserService;
    private readonly ILearningService _learningService;
    private readonly IAchievementRepository _achievementRepository;
    private readonly ICollectionService _collectionService;
    private readonly IOpenAiService _openAiService;
    private readonly ILocalizationResourceManager _resourceManager;
    private readonly TestableMainViewModel _viewModel;
    private readonly TestableSettingsViewModel _settingsViewModel;

    public MainViewModelTests()
    {
        _services = A.Fake<IServiceProvider>();
        _appUserService = A.Fake<IAppUserService>();
        _learningService = A.Fake<ILearningService>();
        _achievementRepository = A.Fake<IAchievementRepository>();
        _collectionService = A.Fake<ICollectionService>();
        _openAiService = A.Fake<IOpenAiService>();
        _resourceManager = A.Fake<ILocalizationResourceManager>();

        _settingsViewModel = new TestableSettingsViewModel(
            _resourceManager,
            _appUserService,
            null!,
            _services,
            _collectionService
        );

        // Passing null for FirebaseAuthClient because we override all its usages
        _viewModel = new TestableMainViewModel(
            _services,
            _appUserService,
            _learningService,
            null!, 
            _achievementRepository,
            _collectionService,
            _openAiService,
            _settingsViewModel);
    }

    private class TestableSettingsViewModel : SettingsViewModel
    {
        public TestableSettingsViewModel(
            ILocalizationResourceManager resourceManager,
            IAppUserService appUserService,
            Firebase.Auth.FirebaseAuthClient authClient,
            IServiceProvider services,
            ICollectionService collectionService)
            : base(resourceManager, appUserService, authClient, services, collectionService)
        {
        }

        protected override string GetPreference(string key, string defaultValue) => defaultValue;
        protected override int GetPreference(string key, int defaultValue) => defaultValue;
        protected override void SetPreference(string key, string value) { }
        protected override void SetPreference(string key, int value) { }
        protected override AppTheme GetAppTheme() => AppTheme.Light;
        protected override void SetAppTheme(AppTheme theme) { }
    }

    private class TestableMainViewModel : MainViewModel
    {
        public bool MockIsAuthenticated { get; set; } = true;
        public string MockDisplayName { get; set; } = "Test User";
        public string MockEmail { get; set; } = "test@example.com";
        public string MockLanguage { get; set; } = "en";
        public bool NavigateToSignInCalled { get; private set; }

        public TestableMainViewModel(
            IServiceProvider services,
            IAppUserService appUserService,
            ILearningService learningService,
            Firebase.Auth.FirebaseAuthClient authClient,
            IAchievementRepository achievementRepository,
            ICollectionService collectionService,
            IOpenAiService openAiService,
            SettingsViewModel settingsViewModel)
            : base(services, appUserService, learningService, authClient, achievementRepository, collectionService, openAiService, settingsViewModel)
        {
        }

        protected override bool IsUserAuthenticated() => MockIsAuthenticated;
        protected override string GetUserDisplayName() => MockDisplayName;
        protected override string GetUserEmail() => MockEmail;
        protected override string GetPreference(string key, string defaultValue) => MockLanguage;
        
        protected override void NavigateToSignIn()
        {
            NavigateToSignInCalled = true;
        }
    }

    [Fact]
    public async Task LoadProfileInfoAsync_ShouldLoadData_WhenUserIsAuthenticated()
    {
        // Arrange
        var user = new AppUser { Id = "1", RequiresAiAnalysis = true };
        A.CallTo(() => _appUserService.GetUserPointsAsync()).Returns(100);
        A.CallTo(() => _learningService.GetCurrentStreakAsync()).Returns(5);
        A.CallTo(() => _appUserService.GetUserBestStreakAsync()).Returns(10);
        A.CallTo(() => _appUserService.GetCurrentUserAsync()).Returns(user);
        A.CallTo(() => _achievementRepository.GetUnlockedAchievementsCountAsync()).Returns(3);
        A.CallTo(() => _openAiService.AnalyzeComprehensiveProfileAsync(A<AppUser>.Ignored, 5, 3, A<IEnumerable<WordCollection>>.Ignored, "en")).Returns("Good job!");

        // Act
        await _viewModel.LoadProfileInfoAsync();

        // Assert
        _viewModel.DisplayName.Should().Be("Test User");
        _viewModel.Email.Should().Be("test@example.com");
        _viewModel.Points.Should().Be(100);
        _viewModel.CurrentStreak.Should().Be(5);
        _viewModel.BestStreak.Should().Be(10);
        _viewModel.IsCurrentStreakBest.Should().BeFalse();
        _viewModel.UnlockedAchievementsCount.Should().Be(3);
        _viewModel.AiFeedback.Should().Be("Good job!");
        _viewModel.IsAiThinking.Should().BeFalse();
    }

    [Fact]
    public async Task LoadProfileInfoAsync_ShouldNavigateToSignIn_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _viewModel.MockIsAuthenticated = false;

        // Act
        await _viewModel.LoadProfileInfoAsync();

        // Assert
        _viewModel.NavigateToSignInCalled.Should().BeTrue();
    }

    [Fact]
    public async Task LoadProfileInfoAsync_ShouldSetIsCurrentStreakBest_WhenStreaksAreEqual()
    {
        // Arrange
        var user = new AppUser { Id = "1" };
        A.CallTo(() => _learningService.GetCurrentStreakAsync()).Returns(10);
        A.CallTo(() => _appUserService.GetUserBestStreakAsync()).Returns(10);
        A.CallTo(() => _appUserService.GetCurrentUserAsync()).Returns(user);

        // Act
        await _viewModel.LoadProfileInfoAsync();

        // Assert
        _viewModel.IsCurrentStreakBest.Should().BeTrue();
    }

    [Fact]
    public async Task GetAiFeedback_ShouldUseCachedFeedback_WhenAnalysisIsNotRequired()
    {
        // Arrange
        var user = new AppUser { Id = "1", RequiresAiAnalysis = false, LastAiAnalysis = "Cached Feedback" };
        A.CallTo(() => _appUserService.GetCurrentUserAsync()).Returns(user);
        
        // Load initial profile to set the user field
        await _viewModel.LoadProfileInfoAsync(); 

        // Act
        await _viewModel.GetAiFeedback();

        // Assert
        _viewModel.AiFeedback.Should().Be("Cached Feedback");
        A.CallTo(() => _openAiService.AnalyzeComprehensiveProfileAsync(A<AppUser>.Ignored, A<int>.Ignored, A<int>.Ignored, A<IEnumerable<WordCollection>>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
    }
}