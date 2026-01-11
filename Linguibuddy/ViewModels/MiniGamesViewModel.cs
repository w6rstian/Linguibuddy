using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;
using Linguibuddy.Views;
using Microsoft.Maui.Controls.Shapes;

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

        [RelayCommand]
        private Task NavigateToAudioQuizAsync()
        {
            return NavigateToGameWithCollectionAsync(nameof(AudioQuizPage));
        }

        [RelayCommand]
        private Task NavigateToImageQuizAsync()
        {
            return NavigateToGameWithCollectionAsync(nameof(ImageQuizPage));
        }

        [RelayCommand]
        private Task NavigateToSentenceQuizAsync()
        {
            return NavigateToGameWithCollectionAsync(nameof(SentenceQuizPage));
        }

        [RelayCommand]
        private Task NavigateToHangman()
        {
            return NavigateToGameWithCollectionAsync(nameof(HangmanPage));
        }

        private async Task<IPopupResult<WordCollection?>> DisplayPopup()
        {
            var popup = new WordCollectionPopup(
                new WordCollectionPopupViewModel(_collectionService, _popupService)
            );

            string colorResourceKey = Application.Current.RequestedTheme == AppTheme.Light ? "Primary" : "PrimaryDark";
            var strokeColor = Application.Current.Resources[colorResourceKey] as Color;

            var shape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(12),
                Stroke = strokeColor,
                StrokeThickness = 2
            };

            var options = new PopupOptions
            {
                Shape = shape
            };

            return await Shell.Current.ShowPopupAsync<WordCollection?>(popup, options);
        }

        private async Task NavigateToGameWithCollectionAsync(string route)
        {
            var result = await DisplayPopup();

            if (result.WasDismissedByTappingOutsideOfPopup || result.Result is null)
                return;

            var selectedCollection = result.Result;

            if (!selectedCollection.Items.Any())
            {
                await Shell.Current.DisplayAlert("Błąd", "Kolekcja jest pusta.", "OK");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "SelectedCollection", selectedCollection }
            };

            await Shell.Current.GoToAsync(route, parameters);
        }
    }
}
