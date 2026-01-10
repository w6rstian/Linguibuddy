using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Linguibuddy.Views;
using Microsoft.Maui.Controls.Shapes;
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

            Shape shape;

            if (Application.Current.RequestedTheme == AppTheme.Light)
            {
                shape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(12),
                    Stroke = Application.Current.Resources["Primary"] as Color,
                    StrokeThickness = 2
                };
            }
            else
            {
                shape = new RoundRectangle
                {
                    CornerRadius = new CornerRadius(12),
                    Stroke = Application.Current.Resources["PrimaryDark"] as Color,
                    StrokeThickness = 2
                };
            }

            var options = new PopupOptions
            {
                Shape = shape 
            };

            return await Shell.Current.ShowPopupAsync<WordCollection?>(popup, options);
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
            var result = await DisplayPopup();

            if (result.WasDismissedByTappingOutsideOfPopup || result.Result is null)
                return;

            var selectedCollection = result.Result;

            var parameters = new Dictionary<string, object>
            {
                { "SelectedCollection", selectedCollection }
            };

            await Shell.Current.GoToAsync(nameof(HangmanPage), parameters);
        }
    }
}
