using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Linguibuddy.Views;

namespace Linguibuddy.ViewModels;

public partial class CollectionsViewModel : ObservableObject
{
    private readonly ICollectionService _collectionService;

    [ObservableProperty] private bool _isSpacedRepetitionEnabled;

    public CollectionsViewModel(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    public ObservableCollection<WordCollection> Collections { get; } = [];

    [RelayCommand]
    public async Task LoadCollections()
    {
        try
        {
            var list = await _collectionService.GetUserCollectionsAsync();
            Collections.Clear();
            foreach (var item in list)
                Collections.Add(item);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task CreateCollection()
    {
        var result = await ShowPromptAsync(
            AppResources.NewCollection,
            AppResources.NameEntry,
            "OK", AppResources.Cancel);

        if (!string.IsNullOrWhiteSpace(result))
        {
            await _collectionService.CreateCollectionAsync(result);
            await LoadCollections();
        }
    }

    [RelayCommand]
    public async Task EditCollection(WordCollection collection)
    {
        if (collection == null) return;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Collection", collection }
        };

        await GoToAsync(nameof(CollectionDetailsPage), navigationParameter);
    }

    [RelayCommand]
    public async Task DeleteCollection(WordCollection collection)
    {
        if (collection == null) return;

        var confirm = await ShowAlertAsync(
            AppResources.RemoveCollection,
            $"{AppResources.RemoveCollectionDesc1} '{collection.Name}' {AppResources.RemoveCollectionDesc2}",
            AppResources.Yes, AppResources.No);

        if (confirm)
        {
            await _collectionService.DeleteCollectionAsync(collection);
            await LoadCollections();
        }
    }

    [RelayCommand]
    public async Task GoToLearning(WordCollection collection)
    {
        if (collection == null || collection.Items == null || collection.Items.Count == 0)
        {
            await ShowAlertAsync(
                AppResources.Error,
                AppResources.CollectionEmpty,
                AppResources.OK);
            return;
        }

        var mode = IsSpacedRepetitionEnabled
            ? LearningMode.SpacedRepetition
            : LearningMode.Standard;

        var navigationParameter = new Dictionary<string, object>
        {
            { "Collection", collection },
            { "Mode", mode }
        };

        await GoToAsync(nameof(FlashcardsPage), navigationParameter);
    }

    protected virtual Task<string> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
    {
        return Shell.Current.DisplayPromptAsync(title, message, accept, cancel);
    }

    protected virtual Task<bool> ShowAlertAsync(string title, string message, string accept, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, accept, cancel);
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