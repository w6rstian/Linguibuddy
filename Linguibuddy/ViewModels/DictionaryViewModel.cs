using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui;
using System.Collections.ObjectModel;
using System.Text;

namespace Linguibuddy.ViewModels
{
    public partial class SearchResultItem : ObservableObject
    {
        public string Word { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Example { get; set; }
        public string Phonetic { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowTranslateButton))]
        private string? _translation;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowTranslateButton))]
        private bool _isBusy;

        public bool ShowTranslateButton => string.IsNullOrEmpty(Translation) && !IsBusy;
    }

    public partial class DictionaryViewModel : ObservableObject
    {
        private readonly DictionaryApiService _dictionaryService;
        private readonly DeepLTranslationService _translationService;
        private readonly OpenAiService _openAiService;
        private readonly FlashcardService _flashcardService;

        [ObservableProperty]
        private string? _inputText;

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<SearchResultItem> SearchResults { get; } = [];

        public ObservableCollection<FlashcardCollection> UserCollections { get; } = [];

        [ObservableProperty]
        private FlashcardCollection? _selectedCollection;

        public DictionaryViewModel(DataContext dataContext,
            DictionaryApiService dictionaryService,
            DeepLTranslationService translationService,
            OpenAiService openAiService,
            FlashcardService flashcardService)
        {
            _dictionaryService = dictionaryService;
            _translationService = translationService;
            _openAiService = openAiService;
            _openAiService = openAiService;
            _flashcardService = flashcardService;

            LoadCollectionsCommand.Execute(null);
        }

        [RelayCommand]
        public async Task LoadCollections()
        {
            try
            {
                var collections = await _flashcardService.GetUserCollectionsAsync();
                UserCollections.Clear();
                foreach (var col in collections)
                {
                    UserCollections.Add(col);
                }

                if (UserCollections.Any())
                    SelectedCollection = UserCollections.First();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania kolekcji: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task LookupWord()
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;

            IsLoading = true;
            SearchResults.Clear();

            try
            {
                var word = InputText.Trim().ToLower();

                var entry = await _dictionaryService.GetEnglishWordAsync(word);

                if (entry != null)
                {
                    foreach (var meaning in entry.Meanings)
                    {
                        foreach (var def in meaning.Definitions)
                        {
                            SearchResults.Add(new SearchResultItem
                            {
                                Word = entry.Word,
                                Phonetic = entry.Phonetic ?? "",
                                PartOfSpeech = meaning.PartOfSpeech,
                                Definition = def.DefinitionText,
                                Example = def.Example,
                                Translation = null,
                                IsBusy = false
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AddItemToFlashcards(SearchResultItem item)
        {
            if (SelectedCollection == null)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Błąd", "Wybierz lub stwórz kolekcję (w zakładce Fiszki), aby zapisać słowo.", "OK");
                return;
            }

            var flashcard = new Flashcard
            {
                Word = item.Word,
                Translation = item.Translation ?? "Brak tłumaczenia",
                PartOfSpeech = item.PartOfSpeech,
                ExampleSentence = item.Example ?? "",
                CollectionId = SelectedCollection.Id
            };

            try
            {
                await _flashcardService.AddFlashcardAsync(flashcard);

                await Application.Current!.Windows[0].Page!.DisplayAlert("Sukces", $"Dodano do: {SelectedCollection.Name}", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Błąd", ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task TranslateItem(SearchResultItem item)
        {
            if (item == null || item.IsBusy) return;

            item.IsBusy = true;

            try
            {
                // DeepL API
                //var translation = await _translationService.TranslateWithContextAsync(
                //    item.Word,
                //    item.Definition,
                //    item.PartOfSpeech,
                //    "PL"
                //);

                // OpenAI
                var translation = await _openAiService.TranslateWithContextAsync(
                    item.Word,
                    item.Definition,
                    item.PartOfSpeech
                );

                item.Translation = translation;
            }
            catch
            {
                item.Translation = "Błąd tłumaczenia";
            }
            finally
            {
                item.IsBusy = false;
            }
        }
    }
}