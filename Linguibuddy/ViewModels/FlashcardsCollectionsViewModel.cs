using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Models;
using Linguibuddy.Services;
using Linguibuddy.Views;
using System.Collections.ObjectModel;

namespace Linguibuddy.ViewModels
{
    public partial class FlashcardsCollectionsViewModel : ObservableObject
    {
        private readonly FlashcardService _flashcardService;

        public ObservableCollection<FlashcardCollection> Collections { get; } = [];

        public FlashcardsCollectionsViewModel(FlashcardService flashcardService)
        {
            _flashcardService = flashcardService;
        }

        [RelayCommand]
        public async Task LoadCollections()
        {
            try
            {
                var list = await _flashcardService.GetUserCollectionsAsync();
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
            // string result = await Shell.Current.DisplayPromptAsync("Nowa kolekcja", "Podaj nazwę:");
            string result = await Application.Current!.Windows[0].Page!.DisplayPromptAsync("Nowa kolekcja", "Podaj nazwę:");
            if (!string.IsNullOrWhiteSpace(result))
            {
                await _flashcardService.CreateCollectionAsync(result);
                await LoadCollections();
            }
        }

        [RelayCommand]
        public async Task GoToLearning(FlashcardCollection collection)
        {
            if (collection == null) return;

            var navigationParameter = new Dictionary<string, object>
            {
                { "Collection", collection }
            };

            await Shell.Current.GoToAsync(nameof(FlashcardsPage), navigationParameter);
        }
    }
}