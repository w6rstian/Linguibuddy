using System.Diagnostics;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Views;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.ViewModels;

public partial class SignInViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly IServiceProvider _services;
    private readonly SettingsViewModel _settingsViewModel;
    private readonly DataContext _db;

    [ObservableProperty] private string _email;

    [ObservableProperty] private float _labelErrorOpacity;

    [ObservableProperty] private string _password;

    public SignInViewModel(FirebaseAuthClient authClient, IServiceProvider services, DataContext dataContext,
        SettingsViewModel settingsViewModel)
    {
        _authClient = authClient;
        _services = services;
        _settingsViewModel = settingsViewModel;
        _db = dataContext;
        LabelErrorOpacity = 0;
    }

    [RelayCommand]
    private async Task SignIn()
    {
        LabelErrorOpacity = 0;
        try
        {
            await SignInWithEmailAndPasswordAsync(Email, Password);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Sign in failed: {ex.Message}");
            LabelErrorOpacity = 1;
            return;
        }

        AppUser? appUser = null;
        try
        {
            appUser = await FindAppUserAsync(GetAuthUserUid());
        }
        catch (SqliteException ex)
        {
            Debug.WriteLine($"Database error: {ex.Message}");
            await ShowAlertAsync(AppResources.Error, AppResources.DatabaseError, AppResources.OK);
            return;
        }

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
    private async Task NavigateSignUp()
    {
        NavigateToSignUpPage();
    }

    protected virtual async Task SignInWithEmailAndPasswordAsync(string email, string password)
    {
        await _authClient.SignInWithEmailAndPasswordAsync(email, password);
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
        var userAchievements = await _db.UserAchievements
            .Where(u => u.AppUserId == userId)
            .ToListAsync();

        foreach (var achievement in allAchievements)
            if (!userAchievements.Exists(ua => ua.AchievementId == achievement.Id))
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

    protected virtual void NavigateToSignUpPage()
    {
        var signUpPage = _services.GetRequiredService<SignUpPage>();
        Application.Current!.Windows[0].Page = new NavigationPage(signUpPage);
    }

    protected virtual Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }
}