using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;
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
        private readonly IPopupService _popupService;
        private readonly CollectionService _collectionService;
        public MiniGamesViewModel(CollectionService collectionService, IPopupService popupService)
        {
            _collectionService = collectionService;
            _popupService = popupService;
        }

        private async Task<IPopupResult<WordCollection?>> DisplayPopup()
        {
            var popup = new WordCollectionPopup(
                new WordCollectionPopupViewModel(_collectionService, _popupService)
                );

            return await Shell.Current.ShowPopupAsync<WordCollection?>(popup);
        }

        [RelayCommand]
        private async Task NavigateToAudioQuizAsync()
        {
            var result = await DisplayPopup();

            if (result.WasDismissedByTappingOutsideOfPopup || result.Result is null)
                return;

            var selectedCollection = result.Result;

            var parameters = new Dictionary<string, object>
            {
                { "SelectedCollection", selectedCollection }
            };

            await Shell.Current.GoToAsync(nameof(AudioQuizPage), parameters);
        }

        [RelayCommand]
        private async Task NavigateToImageQuizAsync()
        {
            var result = await DisplayPopup();

            if (result.WasDismissedByTappingOutsideOfPopup || result.Result is null)
                return;

            var selectedCollection = result.Result;

            var parameters = new Dictionary<string, object>
            {
                { "SelectedCollection", selectedCollection }
            };

            await Shell.Current.GoToAsync(nameof(ImageQuizPage), parameters);
        }

        [RelayCommand]
        private async Task NavigateToSentenceQuizAsync()
        {
            await Shell.Current.GoToAsync(nameof(SentenceQuizPage));
        }

        [RelayCommand]
        private async Task NavigateToHangman()
        {
            await Shell.Current.GoToAsync(nameof(HangmanPage));
        }
    }
}
