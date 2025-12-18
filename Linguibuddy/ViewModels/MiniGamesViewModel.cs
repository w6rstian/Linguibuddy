using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linguibuddy.ViewModels
{
    public partial class MiniGamesViewModel : ObservableObject
    {
        public MiniGamesViewModel()
        { }

        [RelayCommand]
        private async Task NavigateToAudioQuizAsync()
        {
            await Shell.Current.GoToAsync(nameof(AudioQuizPage));
        }

        [RelayCommand]
        private async Task NavigateToImageQuizAsync()
        {
            await Shell.Current.GoToAsync(nameof(ImageQuizPage));
        }
    }
}
