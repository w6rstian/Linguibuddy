using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Linguibuddy.Views;
using Microsoft.Maui.Controls.Shapes;

namespace Linguibuddy.ViewModels;

public partial class MiniGamesViewModel : ObservableObject
{
    private readonly ICollectionService _collectionService;
    private readonly IPopupService _popupService;

    public MiniGamesViewModel(ICollectionService collectionService, IPopupService popupService)
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

    [RelayCommand]
    private Task NavigateToSpeakingQuizAsync()
    {
        return NavigateToGameWithCollectionAsync(nameof(SpeakingQuizPage));
    }

    protected virtual async Task<WordCollection?> GetSelectedCollectionFromPopupAsync()
    {
        var popup = new WordCollectionPopup(
            new WordCollectionPopupViewModel(_collectionService, _popupService)
        );

        var colorResourceKey = Application.Current.RequestedTheme == AppTheme.Light ? "Primary" : "PrimaryDark";
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

        var result = await Shell.Current.ShowPopupAsync<WordCollection?>(popup, options);
        
        if (result.WasDismissedByTappingOutsideOfPopup)
            return null;

        return result.Result;
    }

    private async Task NavigateToGameWithCollectionAsync(string route)
    {
        var selectedCollection = await GetSelectedCollectionFromPopupAsync();

        if (selectedCollection is null)
            return;

        if (!selectedCollection.Items.Any())
        {
            await ShowAlertAsync(AppResources.Error, AppResources.CollectionEmpty, "OK");
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            { "SelectedCollection", selectedCollection }
        };

        await GoToAsync(route, parameters);
    }

    protected virtual Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }

    protected virtual Task GoToAsync(string route, IDictionary<string, object> parameters)
    {
        return Shell.Current.GoToAsync(route, parameters);
    }
}