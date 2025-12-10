using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Services;
using Linguibuddy.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DataContext _dataContext;
        private readonly FirebaseAuthClient _authClient;
        private readonly IServiceProvider _services;
        public ObservableCollection<Models.User> Users { get; set; }

        public MainViewModel(DataContext dataContext, FirebaseAuthClient authClient, IServiceProvider services)
        {
            _dataContext = dataContext;
            _authClient = authClient;
            _services = services;

            Users = [];
            LoadUsers();
        }

        private async void LoadUsers()
        {
            try
            {
                await _dataContext.Database.EnsureCreatedAsync();

                var users = await _dataContext.Users.ToListAsync();
                foreach(var user in users)
                {
                    Users.Add(user);
                }
            }
            catch(Exception ex)
            {
                return;
            }
        }

        [RelayCommand]
        private async Task SignOut()
        {
            _authClient.SignOut();
            var signInPage = _services.GetRequiredService<SignInPage>();
            Application.Current.Windows[0].Page = new NavigationPage(signInPage);
        }
    }
}
