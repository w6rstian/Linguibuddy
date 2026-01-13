using Firebase.Auth;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.Services
{
    public class LearningService
    {
        private readonly IUserLearningDayRepository _repo;
        private readonly IAppUserRepository _appUserRepo;
        private readonly AppUserService _appUserService;
        private readonly FirebaseAuthClient _authClient;

        private readonly string _currentUserId;
        private AppUser _appUser;
        public LearningService(IUserLearningDayRepository repo, IAppUserRepository appUserRepo, AppUserService appUserService, FirebaseAuthClient authClient)
        {
            _repo = repo;
            _appUserRepo = appUserRepo;
            _appUserService = appUserService;
            _authClient = authClient;

            _currentUserId = _authClient.User.Uid;
        }

        public async Task MarkLearnedTodayAsync()
        {
            if (_appUser is null)
            {
                _appUser = await _appUserRepo.GetByIdAsync(_currentUserId)
                    ?? throw new Exception("User not found");
            }

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
            {
                _appUser = await _appUserRepo.GetByIdAsync(_currentUserId)
                    ?? throw new Exception("User not found");
            }

            var dates = await _repo.GetLearningDatesAsync(_currentUserId);

            int streak = 0;
            var expected = DateTime.Today;

            foreach (var date in dates)
            {
                if (date == expected)
                {
                    streak++;
                    expected = expected.AddDays(-1); // -1 because dates are ordered descending, so we're counting back from today
                }
                else
                {
                    break;
                }
            }

            if (streak > await _appUserService.GetUserBestStreakAsync())
            {
                await _appUserService.SetBestLearningStreakAsync(streak);
            }

            return streak;
        }
    }
}
