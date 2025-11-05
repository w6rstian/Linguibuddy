using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DataContext _dataContext;
        public ObservableCollection<User> Users { get; set; }

        public MainViewModel(DataContext dataContext)
        {
            _dataContext = dataContext;
            Users = new ObservableCollection<User>();
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
    }
}
