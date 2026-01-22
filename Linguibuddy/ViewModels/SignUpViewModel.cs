using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Views;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.ViewModels;

public partial class SignUpViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly DataContext _db;
    private readonly IServiceProvider _services;

    [ObservableProperty] private string _email;

    [ObservableProperty] private float _labelEmailErrorOpacity;

    [ObservableProperty] private float _labelPasswordErrorOpacity;

    [ObservableProperty] private float _labelUsernameErrorOpacity;

    [ObservableProperty] private string _password;

    [ObservableProperty] private string _username;

    public SignUpViewModel(FirebaseAuthClient authClient, IServiceProvider services, DataContext dataContext)
    {
        _authClient = authClient;
        _services = services;
        _db = dataContext;
        LabelUsernameErrorOpacity = 0;
        LabelEmailErrorOpacity = 0;
        LabelPasswordErrorOpacity = 0;
    }

    [RelayCommand]
    private async Task SignUp()
    {
        LabelUsernameErrorOpacity = 0;
        LabelEmailErrorOpacity = 0;
        LabelPasswordErrorOpacity = 0;

        if (string.IsNullOrWhiteSpace(Username))
            LabelUsernameErrorOpacity = 1;

        const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (string.IsNullOrWhiteSpace(Email) || !Regex.IsMatch(Email, pattern))
            LabelEmailErrorOpacity = 1;

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            LabelPasswordErrorOpacity = 1;

        if (LabelUsernameErrorOpacity == 1 || LabelEmailErrorOpacity == 1 || LabelPasswordErrorOpacity == 1)
            return;

        await CreateUserWithEmailAndPasswordAsync(Email, Password, Username);

        // This check is excessive but I'd rather have it than not, for safety =)
        var appUser = await FindAppUserAsync(GetAuthUserUid());
        if (appUser == null)
        {
            appUser = new AppUser
            {
                Id = GetAuthUserUid(),
                UserName = GetAuthUserDisplayName()
            };
            await AddAppUserAsync(appUser);

            await InitializeUserAchievementsAsync(appUser.Id);
        }

        NavigateToMainPage();
    }

    [RelayCommand]
    private async Task NavigateSignIn()
    {
        NavigateToSignInPage();
    }

    protected virtual async Task CreateUserWithEmailAndPasswordAsync(string email, string password, string username)
    {
        await _authClient.CreateUserWithEmailAndPasswordAsync(email, password, username);
    }

    protected virtual string GetAuthUserUid()
    {
        return _authClient.User.Uid;
    }

    protected virtual string GetAuthUserDisplayName()
    {
        return _authClient.User.Info.DisplayName;
    }

    protected virtual async Task<AppUser?> FindAppUserAsync(string uid)
    {
        return await _db.AppUsers.FindAsync(uid);
    }

    protected virtual async Task AddAppUserAsync(AppUser appUser)
    {
        _db.AppUsers.Add(appUser);
        await _db.SaveChangesAsync();
    }

    protected virtual async Task InitializeUserAchievementsAsync(string userId)
    {
        var allAchievements = await _db.Achievements.ToListAsync();

        foreach (var achievement in allAchievements)
        {
            var userAchievement = new UserAchievement
            {
                AppUserId = userId,
                AchievementId = achievement.Id,
                IsUnlocked = false
            };
            _db.UserAchievements.Add(userAchievement);
        }

        await _db.SaveChangesAsync();
    }

    protected virtual void NavigateToMainPage()
    {
        Application.Current!.Windows[0].Page = App.GetMainShell();
    }

    protected virtual void NavigateToSignInPage()
    {
        var signInPage = _services.GetRequiredService<SignInPage>();
        Application.Current!.Windows[0].Page = new NavigationPage(signInPage);
    }
}