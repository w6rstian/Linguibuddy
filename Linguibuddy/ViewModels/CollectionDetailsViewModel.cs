using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Helpers;

namespace Linguibuddy.ViewModels;

[QueryProperty(nameof(Collection), "Collection")]
public partial class CollectionDetailsViewModel : ObservableObject
{
    private readonly ICollectionService _collectionService;
    private readonly IOpenAiService _openAiService;
    private readonly IAppUserService _appUserService;

    [ObservableProperty]
    private WordCollection? _collection;

    [ObservableProperty]
    private string _aiFeedback = "Analizuję Twoje postępy...";

    public ObservableCollection<CollectionItem> Items { get; } = [];

    public CollectionDetailsViewModel(
        ICollectionService collectionService,
        IOpenAiService openAiService,
        IAppUserService appUserService)
    {
        _collectionService = collectionService;
        _openAiService = openAiService;
        _appUserService = appUserService;
    }

    partial void OnCollectionChanged(WordCollection? value)
    {
        LoadItems();
        Task.Run(LoadAiFeedback);
    }

    private async Task LoadAiFeedback()
    {
        if (Collection == null) return;

        if (!Collection.RequiresAiAnalysis && !string.IsNullOrEmpty(Collection.LastAiAnalysis))
        {
            AiFeedback = Collection.LastAiAnalysis;
            return;
        }

        AiFeedback = "Analizuję Twoje postępy...";
        
        try
        {
            var difficulty = await _appUserService.GetUserDifficultyAsync();
            var language = Preferences.Default.Get(Constants.LanguageKey, "pl");
            var feedback = await _openAiService.AnalyzeCollectionProgressAsync(Collection, difficulty, language);
            
            AiFeedback = feedback;

            Collection.LastAiAnalysis = feedback;
            Collection.RequiresAiAnalysis = false;
            await _collectionService.UpdateCollectionAsync(Collection);
        }
        catch (Exception)
        {
            AiFeedback = "Nie udało się pobrać analizy AI.";
        }
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
        }
    }

    [RelayCommand]
    public async Task DeleteItem(CollectionItem item)
    {
        if (item == null || Collection == null) return;

        var confirm = await Shell.Current.DisplayAlert(
            AppResources.Delete,
            $"Czy na pewno chcesz usunąć słowo '{item.Word}'?",
            AppResources.Yes, AppResources.No);

        if (confirm)
        {
            await _collectionService.DeleteCollectionItemAsync(item);
            Collection.Items.Remove(item);
            Items.Remove(item);
        }
    }
}
