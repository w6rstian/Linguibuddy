using Firebase.Auth;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;

namespace Linguibuddy.Services;

public class LearningService : ILearningService
{
    private readonly IAppUserRepository _appUserRepo;
    private readonly IAppUserService _appUserService;
    private readonly IAuthService _authService;

    private readonly string _currentUserId;
    private readonly IUserLearningDayRepository _repo;
    private AppUser _appUser;

    public LearningService(IUserLearningDayRepository repo, IAppUserRepository appUserRepo,
        IAppUserService appUserService, IAuthService authService)
    {
        _repo = repo;
        _appUserRepo = appUserRepo;
        _appUserService = appUserService;
        _authService = authService;

        _currentUserId = _authService.CurrentUserId;
    }

    public async Task MarkLearnedTodayAsync()
    {
        if (_appUser is null)
            _appUser = await _appUserRepo.GetByIdAsync(_currentUserId)
                       ?? throw new Exception("User not found");

        var today = DateTime.Today;

        if (await _repo.ExistsAsync(_currentUserId, today))
            return;

        await _repo.AddAsync(new UserLearningDay
        {
            AppUserId = _currentUserId,
            Date = today,
            Learned = true
        });
    }

    public async Task<int> GetCurrentStreakAsync()
    {
        if (_appUser is null)
            _appUser = await _appUserRepo.GetByIdAsync(_currentUserId)
                       ?? throw new Exception("User not found");

        var dates = await _repo.GetLearningDatesAsync(_currentUserId);

        var streak = 0;
        var expected = DateTime.Today;

        foreach (var date in dates)
            if (date == expected)
            {
                streak++;
                expected = expected
                    .AddDays(-1); // -1 because dates are ordered descending, so we're counting back from today
            }
            else
            {
                break;
            }

        if (streak > await _appUserService.GetUserBestStreakAsync())
            await _appUserService.SetBestLearningStreakAsync(streak);

        return streak;
    }
}