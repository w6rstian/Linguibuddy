using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;

namespace Linguibuddy.ViewModels;

[QueryProperty(nameof(Collection), "Collection")]
public partial class CollectionDetailsViewModel : ObservableObject
{
    private readonly ICollectionService _collectionService;

    [ObservableProperty]
    private WordCollection? _collection;

    public ObservableCollection<CollectionItem> Items { get; } = [];

    public CollectionDetailsViewModel(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    partial void OnCollectionChanged(WordCollection? value)
    {
        LoadItems();
    }

    private void LoadItems()
    {
        Items.Clear();
        if (Collection?.Items != null)
        {
            foreach (var item in Collection.Items)
            {
                Items.Add(item);
            }
        }
    }

    [RelayCommand]
    public async Task RenameCollection()
    {
        if (Collection == null) return;

        var result = await Shell.Current.DisplayPromptAsync(
            AppResources.EditCollection,
            $"{AppResources.Rename} :",
            AppResources.Save, AppResources.Cancel,
            initialValue: Collection.Name);

        if (!string.IsNullOrWhiteSpace(result) && result != Collection.Name)
        {
            await _collectionService.RenameCollectionAsync(Collection, result);
            // Collection name updates automatically via ObservableObject if bound correctly, 
            // but we might need to notify change if it's not. 
            // WordCollection implements ObservableObject so it should update UI.
        }
    }

    [RelayCommand]
    public async Task DeleteItem(CollectionItem item)
    {
        if (item == null || Collection == null) return;

        var confirm = await Shell.Current.DisplayAlert(
            AppResources.Delete, // Using generic Delete if specific string not available
            $"Czy na pewno chcesz usunąć słowo '{item.Word}'?", // Hardcoded Polish as fallback or check resources later
            AppResources.Yes, AppResources.No);

        if (confirm)
        {
            await _collectionService.DeleteCollectionItemAsync(item);
            Collection.Items.Remove(item); // Update the source list
            Items.Remove(item); // Update the UI list
        }
    }
}
