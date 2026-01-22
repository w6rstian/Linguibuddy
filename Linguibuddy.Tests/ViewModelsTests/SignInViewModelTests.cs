using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class SignInViewModelTests
{
    private readonly IServiceProvider _services;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly DataContext _dataContext;
    private readonly TestableSignInViewModel _viewModel;

    public SignInViewModelTests()
    {
        _services = A.Fake<IServiceProvider>();
        
        var resourceManager = A.Fake<LocalizationResourceManager.Maui.ILocalizationResourceManager>();
        var appUserService = A.Fake<IAppUserService>();
        var collectionService = A.Fake<ICollectionService>();
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        _settingsViewModel = A.Fake<SettingsViewModel>(x => x.WithArgumentsForConstructor(new object[] { 
            resourceManager, 
            appUserService, 
            null!, 
            _services, 
            collectionService 
        }));
        
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: "TestSignInDb")
            .Options;
        _dataContext = new DataContext(options);

        
        _viewModel = new TestableSignInViewModel(null!, _services, _dataContext, _settingsViewModel);
    }

    private class TestableSignInViewModel : SignInViewModel
    {
        public bool MockSignInSuccess { get; set; } = true;
        public string MockAuthUid { get; set; } = "uid123";
        public string MockAuthDisplayName { get; set; } = "TestUser";
        public AppUser? MockAppUser { get; set; }
        public bool FindAppUserCalled { get; private set; }
        public bool AddAppUserCalled { get; private set; }
        public bool InitializeAchievementsCalled { get; private set; }
        public bool NavigateToMainPageCalled { get; private set; }
        public bool NavigateToSignUpPageCalled { get; private set; }
        public string? LastAlertMessage { get; private set; }
        public bool ThrowDbException { get; set; }

        public TestableSignInViewModel(Firebase.Auth.FirebaseAuthClient authClient, IServiceProvider services, DataContext dataContext, SettingsViewModel settingsViewModel) 
            : base(authClient, services, dataContext, settingsViewModel)
        {
        }

        protected override Task SignInWithEmailAndPasswordAsync(string email, string password)
        {
            if (MockSignInSuccess) return Task.CompletedTask;
            throw new Exception("Auth Failed");
        }

        protected override string GetAuthUserUid() => MockAuthUid;
        protected override string GetAuthUserDisplayName() => MockAuthDisplayName;

        protected override Task<AppUser?> FindAppUserAsync(string uid)
        {
            if (ThrowDbException) throw new SqliteException("DB Error", 1);
            FindAppUserCalled = true;
            return Task.FromResult(MockAppUser);
        }

        protected override Task AddAppUserAsync(AppUser appUser)
        {
            AddAppUserCalled = true;
            return Task.CompletedTask;
        }

        protected override Task InitializeUserAchievementsAsync(string userId)
        {
            InitializeAchievementsCalled = true;
            return Task.CompletedTask;
        }

        protected override void NavigateToMainPage()
        {
            NavigateToMainPageCalled = true;
        }

        protected override void NavigateToSignUpPage()
        {
            NavigateToSignUpPageCalled = true;
        }

        protected override Task ShowAlertAsync(string title, string message, string cancel)
        {
            LastAlertMessage = message;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task SignIn_ShouldSetErrorOpacity_WhenAuthFails()
    {
        // Arrange
        _viewModel.MockSignInSuccess = false;
        _viewModel.Email = "test@test.com";
        _viewModel.Password = "password";

        // Act
        await _viewModel.SignInCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LabelErrorOpacity.Should().Be(1);
        _viewModel.NavigateToMainPageCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SignIn_ShouldNavigateToMain_WhenAuthSuccessAndUserExists()
    {
        // Arrange
        _viewModel.MockSignInSuccess = true;
        _viewModel.Email = "test@test.com";
        _viewModel.Password = "password";
        _viewModel.MockAppUser = new AppUser { Id = "uid123" };

        // Act
        await _viewModel.SignInCommand.ExecuteAsync(null);

        // Assert
        _viewModel.FindAppUserCalled.Should().BeTrue();
        _viewModel.AddAppUserCalled.Should().BeFalse();
        _viewModel.NavigateToMainPageCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SignIn_ShouldCreateUserAndNavigate_WhenUserDoesNotExist()
    {
        // Arrange
        _viewModel.MockSignInSuccess = true;
        _viewModel.Email = "test@test.com";
        _viewModel.Password = "password";
        _viewModel.MockAppUser = null; 

        // Act
        await _viewModel.SignInCommand.ExecuteAsync(null);

        // Assert
        _viewModel.FindAppUserCalled.Should().BeTrue();
        _viewModel.AddAppUserCalled.Should().BeTrue();
        _viewModel.InitializeAchievementsCalled.Should().BeTrue();
        _viewModel.NavigateToMainPageCalled.Should().BeTrue();
    }

    [Fact]
    public async Task SignIn_ShouldShowAlert_WhenDbExceptionOccurs()
    {
        // Arrange
        _viewModel.MockSignInSuccess = true;
        _viewModel.Email = "test@test.com";
        _viewModel.Password = "password";
        _viewModel.ThrowDbException = true;

        // Act
        await _viewModel.SignInCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastAlertMessage.Should().NotBeNullOrEmpty();
        _viewModel.NavigateToMainPageCalled.Should().BeFalse();
    }

    [Fact]
    public async Task NavigateSignUp_ShouldNavigate()
    {
        // Act
        await _viewModel.NavigateSignUpCommand.ExecuteAsync(null);

        // Assert
        _viewModel.NavigateToSignUpPageCalled.Should().BeTrue();
    }
}
