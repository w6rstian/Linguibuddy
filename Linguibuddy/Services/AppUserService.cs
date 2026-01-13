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
    public class AppUserService
    {
        private readonly IAppUserRepository _appUsers;
        private readonly FirebaseAuthClient _authClient;

        private readonly string _currentUserId;
        private AppUser _appUser;

        public AppUserService(IAppUserRepository appUsers, FirebaseAuthClient authClient)
        {
            _appUsers = appUsers;
            _authClient = authClient;
            _currentUserId = authClient.User.Uid;
        }

        public async Task AddUserPointsAsync(int points)
        {
            if (_appUser is null)
            {
                _appUser = await _appUsers.GetByIdAsync(_currentUserId)
                    ?? throw new Exception("User not found");
            }

            _appUser.Points += points;

            await _appUsers.SaveChangesAsync();
        }

        public async Task<int> GetUserPointsAsync()
        {
            if (_appUser is null)
            {
                _appUser = await _appUsers.GetByIdAsync(_currentUserId)
                    ?? throw new Exception("User not found");
            }

            return _appUser.Points;
        }

        public async Task<int> GetUserBestStreakAsync()
        {
            if (_appUser is null)
            {
                _appUser = await _appUsers.GetByIdAsync(_currentUserId)
                    ?? throw new Exception("User not found");
            }

            return _appUser.BestLearningStreak;
        }

        public async Task SetBestLearningStreakAsync(int newStreak)
        {
            if (_appUser is null)
            {
                _appUser = await _appUsers.GetByIdAsync(_currentUserId)
                    ?? throw new Exception("User not found");
            }

            _appUser.BestLearningStreak = newStreak;
            await _appUsers.SaveChangesAsync();
        }
    }
}
