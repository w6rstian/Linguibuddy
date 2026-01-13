using CommunityToolkit.Mvvm.ComponentModel;
using Linguibuddy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly AppUserService _appUserService;

        [ObservableProperty]
        private string _displayName;

        public ProfileViewModel(AppUserService appUserService)
        {
            _appUserService = appUserService;
        }

        
    }
}
