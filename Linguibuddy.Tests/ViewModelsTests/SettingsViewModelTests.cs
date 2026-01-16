using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using LocalizationResourceManager.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class SettingsViewModelTests
{
    private readonly ILocalizationResourceManager _resourceManager;
    private readonly IAppUserService _appUserService;
    private readonly IServiceProvider _services;
    private readonly ICollectionService _collectionService;
    private readonly TestableSettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _resourceManager = A.Fake<ILocalizationResourceManager>();
        _appUserService = A.Fake<IAppUserService>();
        _services = A.Fake<IServiceProvider>();
        _collectionService = A.Fake<ICollectionService>();

        _viewModel = new TestableSettingsViewModel(
            _resourceManager,
            _appUserService,
            null!, // FirebaseAuthClient is handled via virtual methods if needed, but here mostly irrelevant for logic except SignOut
            _services,
            _collectionService);
    }

    private class TestableSettingsViewModel : SettingsViewModel
    {
        public Dictionary<string, object> MockPreferences { get; } = new();
        public AppTheme MockAppTheme { get; set; } = AppTheme.Light;
        public bool NavigateToSignInCalled { get; private set; }

        public TestableSettingsViewModel(
            ILocalizationResourceManager resourceManager,
            IAppUserService appUserService,
            Firebase.Auth.FirebaseAuthClient authClient,
            IServiceProvider services,
            ICollectionService collectionService)
            : base(resourceManager, appUserService, authClient, services, collectionService)
        {
        }

        protected override string GetPreference(string key, string defaultValue)
        {
            return MockPreferences.TryGetValue(key, out var value) ? (string)value : defaultValue;
        }

        protected override int GetPreference(string key, int defaultValue)
        {
            return MockPreferences.TryGetValue(key, out var value) ? (int)value : defaultValue;
        }

        protected override void SetPreference(string key, string value)
        {
            MockPreferences[key] = value;
        }

        protected override void SetPreference(string key, int value)
        {
            MockPreferences[key] = value;
        }

        protected override AppTheme GetAppTheme() => MockAppTheme;

        protected override void SetAppTheme(AppTheme theme)
        {
            MockAppTheme = theme;
        }

        protected override void NavigateToSignIn()
        {
            NavigateToSignInCalled = true;
        }

        protected override void RunInBackground(Func<Task> action)
        {
            action().GetAwaiter().GetResult(); // Run synchronously for tests
        }
    }

    [Fact]
    public async Task LoadDifficultyAsync_ShouldSetSelectedDifficulty()
    {
        // Arrange
        A.CallTo(() => _appUserService.GetUserDifficultyAsync()).Returns(DifficultyLevel.B2);

        // Act
        await _viewModel.LoadDifficultyAsync();

        // Assert
        _viewModel.SelectedDifficulty.Should().Be(DifficultyLevel.B2);
    }

    [Fact]
    public async Task LoadLessonLengthAsync_ShouldSetSelectedLessonLength()
    {
        // Arrange
        A.CallTo(() => _appUserService.GetUserLessonLengthAsync()).Returns(20);

        // Act
        await _viewModel.LoadLessonLengthAsync();

        // Assert
        _viewModel.SelectedLessonLength.Should().Be(20);
    }

    [Fact]
    public void SelectedDifficulty_Changed_ShouldCallSetUserDifficulty()
    {
        // Act
        _viewModel.SelectedDifficulty = DifficultyLevel.C1;

        // Assert
        A.CallTo(() => _appUserService.SetUserDifficultyAsync(DifficultyLevel.C1)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void SelectedLessonLength_Changed_ShouldCallSetUserLessonLength()
    {
        // Act
        _viewModel.SelectedLessonLength = 50;

        // Assert
        A.CallTo(() => _appUserService.SetUserLessonLengthAsync(50)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ChangeLanguage_ShouldToggleLanguageAndSavePreference()
    {
        // Arrange
        _viewModel.MockPreferences[Constants.LanguageKey] = "pl";
        A.CallTo(() => _resourceManager.CurrentCulture).Returns(CultureInfo.GetCultureInfo("pl"));

        // Act
        await _viewModel.ChangeLanguageCommand.ExecuteAsync(null);

        // Assert
        _viewModel.MockPreferences[Constants.LanguageKey].Should().Be("en");
        A.CallToSet(() => _resourceManager.CurrentCulture).To(CultureInfo.GetCultureInfo("en")).MustHaveHappened();
    }

    [Fact]
    public void ChangeTheme_ShouldToggleThemeAndSavePreference()
    {
        // Arrange
        _viewModel.MockAppTheme = AppTheme.Light;

        // Act
        _viewModel.ChangeThemeCommand.Execute(null);

        // Assert
        _viewModel.MockAppTheme.Should().Be(AppTheme.Dark);
        _viewModel.MockPreferences[Constants.AppThemeKey].Should().Be((int)AppTheme.Dark);
    }

    [Fact]
    public void ChangeTranslationApi_ShouldToggleProviderAndSavePreference()
    {
        // Arrange
        _viewModel.MockPreferences[Constants.TranslationApiKey] = (int)TranslationProvider.OpenAi;

        // Act
        _viewModel.ChangeTranslationApiCommand.Execute(null);

        // Assert
        _viewModel.MockPreferences[Constants.TranslationApiKey].Should().Be((int)TranslationProvider.DeepL);
        _viewModel.TranslationApiName.Should().Be("DeepL");
    }
}
