using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class SignUpViewModelTests
{
    private readonly IServiceProvider _services;
    private readonly DataContext _dataContext;
    private readonly TestableSignUpViewModel _viewModel;

    public SignUpViewModelTests()
    {
        _services = A.Fake<IServiceProvider>();
        
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName: "TestSignUpDb")
            .Options;
        _dataContext = new DataContext(options);

        _viewModel = new TestableSignUpViewModel(null!, _services, _dataContext);
    }

    private class TestableSignUpViewModel : SignUpViewModel
    {
        public bool MockCreateUserSuccess { get; set; } = true;
        public string MockAuthUid { get; set; } = "uid123";
        public string MockAuthDisplayName { get; set; } = "TestUser";
        public AppUser? MockAppUser { get; set; }
        public bool AddAppUserCalled { get; private set; }
        public bool InitializeAchievementsCalled { get; private set; }
        public bool NavigateToMainPageCalled { get; private set; }
        public bool NavigateToSignInPageCalled { get; private set; }

        public TestableSignUpViewModel(Firebase.Auth.FirebaseAuthClient authClient, IServiceProvider services, DataContext dataContext) 
            : base(authClient, services, dataContext)
        {
        }

        protected override Task CreateUserWithEmailAndPasswordAsync(string email, string password, string username)
        {
            if (MockCreateUserSuccess) return Task.CompletedTask;
            // Simplified: real VM logic doesn't explicitly catch this, so exception would propagate.
            // But here we assume successful flow logic check.
            throw new System.Exception("Create failed");
        }

        protected override string GetAuthUserUid() => MockAuthUid;
        protected override string GetAuthUserDisplayName() => MockAuthDisplayName;

        protected override Task<AppUser?> FindAppUserAsync(string uid)
        {
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

        protected override void NavigateToSignInPage()
        {
            NavigateToSignInPageCalled = true;
        }
    }

    [Fact]
    public async Task SignUp_ShouldValidateEmptyUsername()
    {
        // Arrange
        _viewModel.Username = "";
        _viewModel.Email = "test@test.com";
        _viewModel.Password = "password123";

        // Act
        await _viewModel.SignUpCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LabelUsernameErrorOpacity.Should().Be(1);
        _viewModel.NavigateToMainPageCalled.Should().BeFalse();
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("#@%^%#$@#$@#.com")]
    [InlineData("@example.com")]
    [InlineData("Joe Smith <email@example.com>")]
    [InlineData("email.example.com")]
    [InlineData("email@example@example.com")]
    [InlineData("email@example")]
    [InlineData("")]
    [InlineData(null)]
    public async Task SignUp_ShouldValidateInvalidEmail(string invalidEmail)
    {
        // Arrange
        _viewModel.Username = "User";
        _viewModel.Email = invalidEmail;
        _viewModel.Password = "password123";

        // Act
        await _viewModel.SignUpCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LabelEmailErrorOpacity.Should().Be(1);
        _viewModel.NavigateToMainPageCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SignUp_ShouldValidateShortPassword()
    {
        // Arrange
        _viewModel.Username = "User";
        _viewModel.Email = "test@test.com";
        _viewModel.Password = "123";

        // Act
        await _viewModel.SignUpCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LabelPasswordErrorOpacity.Should().Be(1);
        _viewModel.NavigateToMainPageCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SignUp_ShouldCreateUserAndNavigate_WhenValidationPasses()
    {
        // Arrange
        _viewModel.Username = "User";
        _viewModel.Email = "test@test.com";
        _viewModel.Password = "password123";
        _viewModel.MockAppUser = null; // User doesn't exist yet

        // Act
        await _viewModel.SignUpCommand.ExecuteAsync(null);

        // Assert
        _viewModel.AddAppUserCalled.Should().BeTrue();
        _viewModel.InitializeAchievementsCalled.Should().BeTrue();
        _viewModel.NavigateToMainPageCalled.Should().BeTrue();
        _viewModel.LabelUsernameErrorOpacity.Should().Be(0);
        _viewModel.LabelEmailErrorOpacity.Should().Be(0);
        _viewModel.LabelPasswordErrorOpacity.Should().Be(0);
    }

    [Fact]
    public async Task NavigateSignIn_ShouldNavigate()
    {
        // Act
        await _viewModel.NavigateSignInCommand.ExecuteAsync(null);

        // Assert
        _viewModel.NavigateToSignInPageCalled.Should().BeTrue();
    }
}
