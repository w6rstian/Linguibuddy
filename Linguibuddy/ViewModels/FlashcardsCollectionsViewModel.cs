using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Linguibuddy.Views;
using System.Collections.ObjectModel;

namespace Linguibuddy.ViewModels
{
    public partial class FlashcardsCollectionsViewModel : ObservableObject
    {
        private readonly CollectionService _collectionService;

        public ObservableCollection<WordCollection> Collections { get; } = [];

        [ObservableProperty]
        private bool _isSpacedRepetitionEnabled;

        public FlashcardsCollectionsViewModel(CollectionService collectionService)
        {
            _collectionService = collectionService;
        }

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
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task CreateCollection()
        {
            string result = await Shell.Current.DisplayPromptAsync(
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

            string result = await Shell.Current.DisplayPromptAsync(
                AppResources.EditCollection,
                $"{AppResources.Rename} :",
                AppResources.Save, AppResources.Cancel,
                initialValue: collection.Name);

            if (!string.IsNullOrWhiteSpace(result) && result != collection.Name)
            {
                await _collectionService.RenameCollectionAsync(collection, result);

                // jesli wordcollection nie jest observable, to trzeba odświeżyć listę (i tk się nie zmienia dziadostwo)
                //await LoadCollections();

                // jesli jest observable, to wystarczy zmienić nazwę (nie wiem czy tak się robi ale działa)
                collection.Name = result;
            }
        }

        [RelayCommand]
        public async Task DeleteCollection(WordCollection collection)
        {
            if (collection == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
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
            if (collection == null) return;

            LearningMode mode = IsSpacedRepetitionEnabled
                ? LearningMode.SpacedRepetition
                : LearningMode.Standard;

            var navigationParameter = new Dictionary<string, object>
            {
                { "Collection", collection },
                { "Mode", mode }
            };

            await Shell.Current.GoToAsync(nameof(FlashcardsPage), navigationParameter);
        }
    }
}