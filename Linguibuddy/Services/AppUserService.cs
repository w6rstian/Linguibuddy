using Firebase.Auth;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;

namespace Linguibuddy.Services;

public class AppUserService : IAppUserService
{
    private readonly IAppUserRepository _appUsers;
    private readonly IAuthService _authService;

    private readonly string _currentUserId;
    private AppUser _appUser;

    public AppUserService(IAppUserRepository appUsers, IAuthService authService)
    {
        _appUsers = appUsers;
        _authService = authService;
        _currentUserId = authService.CurrentUserId;
    }

    private async Task EnsureUserLoadedAsync()
    {
        if (_appUser is null)
        {
            _appUser = await _appUsers.GetByIdAsync(_currentUserId)
                       ?? throw new Exception("User not found");
        }
    }

    public async Task AddUserPointsAsync(int points)
    {
        await EnsureUserLoadedAsync();

        _appUser.Points += points;

        await _appUsers.SaveChangesAsync();
    }

    public async Task<int> GetUserPointsAsync()
    {
        await EnsureUserLoadedAsync();
        return _appUser.Points;
    }

    public async Task<DifficultyLevel> GetUserDifficultyAsync()
    {
        await EnsureUserLoadedAsync();
        return _appUser.DifficultyLevel;
    }

    public async Task SetUserDifficultyAsync(DifficultyLevel level)
    {
        await EnsureUserLoadedAsync();

        _appUser.DifficultyLevel = level;
        await _appUsers.SaveChangesAsync();
    }

    public async Task<int> GetUserLessonLengthAsync()
    {
        await EnsureUserLoadedAsync();
        return _appUser.LessonLength;
    }

    public async Task SetUserLessonLengthAsync(int length)
    {
        await EnsureUserLoadedAsync();

        _appUser.LessonLength = length;
        await _appUsers.SaveChangesAsync();
    }

    public async Task<int> GetUserBestStreakAsync()
    {
        await EnsureUserLoadedAsync();
        return _appUser.BestLearningStreak;
    }

    public async Task SetBestLearningStreakAsync(int newStreak)
    {
        await EnsureUserLoadedAsync();

        _appUser.BestLearningStreak = newStreak;
        await _appUsers.SaveChangesAsync();
    }

    public async Task MarkAiAnalysisRequiredAsync()
    {
        await EnsureUserLoadedAsync();

        _appUser.RequiresAiAnalysis = true;
        await _appUsers.SaveChangesAsync();
    }

    public async Task UpdateAppUserAsync(AppUser user)
    {
        _appUsers.Update(user);
        await _appUsers.SaveChangesAsync();
        _appUser = user;
    }

    public async Task<AppUser> GetCurrentUserAsync()
    {
        await EnsureUserLoadedAsync();
        return _appUser;
    }
}