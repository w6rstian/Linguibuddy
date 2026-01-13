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

        public LearningService(IUserLearningDayRepository repo)
        {
            _repo = repo;
        }

        public async Task MarkLearnedTodayAsync(string userId)
        {
            var today = DateTime.Today;

            if (await _repo.ExistsAsync(userId, today))
                return;

            await _repo.AddAsync(new UserLearningDay
            {
                AppUserId = userId,
                Date = today,
                Learned = true
            });
        }

        public async Task<int> GetCurrentStreakAsync(string userId)
        {
            var dates = await _repo.GetLearningDatesAsync(userId);

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

            // TODO: Save max streak to AppUser maybe??

            return streak;
        }
    }
}
