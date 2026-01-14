using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.ViewModels;

public partial class WordCollectionPopupViewModel : ObservableObject
{
    private readonly ICollectionService _collectionService;
    private readonly IPopupService _popupService;

    [ObservableProperty] private IEnumerable<WordCollection> _collections;

    [ObservableProperty] private WordCollection _selectedCollection;

    public WordCollectionPopupViewModel(ICollectionService collectionService, IPopupService popupService)
    {
        _collectionService = collectionService;
        _popupService = popupService;

        LoadCollectionsAsync();
    }

    private async Task LoadCollectionsAsync()
    {
        Collections = await _collectionService.GetUserCollectionsAsync();
    }

    [RelayCommand]
    private async Task CollectionSelected(WordCollection? selected)
    {
        await _popupService.ClosePopupAsync<WordCollection?>(Shell.Current, selected);
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await _popupService.ClosePopupAsync<WordCollection?>(Shell.Current, null);
    }
}