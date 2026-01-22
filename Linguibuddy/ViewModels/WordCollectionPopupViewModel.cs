using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;

namespace Linguibuddy.ViewModels;

public partial class WordCollectionPopupViewModel : ObservableObject
{
    private readonly ICollectionService _collectionService;
    private readonly IPopupService _popupService;

    [ObservableProperty] private IEnumerable<WordCollection> _collections = [];

    [ObservableProperty] private WordCollection? _selectedCollection;

    public WordCollectionPopupViewModel(ICollectionService collectionService, IPopupService popupService)
    {
        _collectionService = collectionService;
        _popupService = popupService;

        RunInBackground(async () => await LoadCollectionsAsync());
    }

    public async Task LoadCollectionsAsync()
    {
        Collections = await _collectionService.GetUserCollectionsAsync();
    }

    [RelayCommand]
    private async Task CollectionSelected(WordCollection? selected)
    {
        await ClosePopup(selected);
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await ClosePopup(null);
    }

    protected virtual async Task ClosePopup(WordCollection? result)
    {
        await _popupService.ClosePopupAsync<WordCollection?>(Shell.Current, result);
    }

    protected virtual void RunInBackground(Func<Task> action)
    {
        Task.Run(action);
    }
}