using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Data;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Plugin.Maui.Audio;
using System.Collections.ObjectModel;

namespace Linguibuddy.ViewModels
{
    public partial class SearchResultItem : ObservableObject
    {
        public string Word { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;
        public string Definition { get; set; } = string.Empty;
        public string? Example { get; set; }
        public string Phonetic { get; set; } = string.Empty;
        public string? AudioUrl { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowTranslateButton))]
        private string? _translation;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowTranslateButton))]
        private bool _isBusy;

        public bool ShowTranslateButton => string.IsNullOrEmpty(Translation) && !IsBusy;

        public bool HasAudio => !string.IsNullOrWhiteSpace(AudioUrl);
    }

    public partial class DictionaryViewModel : ObservableObject
    {
        private readonly DictionaryApiService _dictionaryService;
        private readonly DeepLTranslationService _translationService;
        private readonly OpenAiService _openAiService;
        private readonly FlashcardService _flashcardService;
        private readonly IAudioManager _audioManager;

        [ObservableProperty]
        private string? _inputText;

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<SearchResultItem> SearchResults { get; } = [];

        public ObservableCollection<FlashcardCollection> UserCollections { get; } = [];

        [ObservableProperty]
        private FlashcardCollection? _selectedCollection;

        private IAudioPlayer? _audioPlayer;

        public DictionaryViewModel(DataContext dataContext,
            DictionaryApiService dictionaryService,
            DeepLTranslationService translationService,
            OpenAiService openAiService,
            FlashcardService flashcardService,
            IAudioManager audioManager)
        {
            _dictionaryService = dictionaryService;
            _translationService = translationService;
            _openAiService = openAiService;
            _openAiService = openAiService;
            _flashcardService = flashcardService;
            _audioManager = audioManager;

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
                    string finalAudio = "";
                    string finalPhoneticText = "";

                    var perfectMatch = entry.Phonetics
                        .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text) && !string.IsNullOrWhiteSpace(p.Audio));

                    if (perfectMatch != null)
                    {
                        finalAudio = perfectMatch.Audio;
                        finalPhoneticText = perfectMatch.Text;
                    }
                    else
                    {
                        finalAudio = entry.Phonetics
                            .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Audio))?.Audio ?? "";

                        finalPhoneticText = entry.Phonetics
                                        .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text))?.Text
                                    ?? entry.Phonetic
                                    ?? "";
                    }

                    foreach (var meaning in entry.Meanings)
                    {
                        foreach (var def in meaning.Definitions)
                        {
                            SearchResults.Add(new SearchResultItem
                            {
                                Word = entry.Word,
                                Phonetic = finalPhoneticText,
                                AudioUrl = finalAudio,
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
        public async Task PlayAudio(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            if (_audioPlayer != null && _audioPlayer.IsPlaying)
            {
                _audioPlayer.Dispose();
            }

            try
            {
                using var client = new HttpClient();
                var audioBytes = await client.GetByteArrayAsync(url);

                string fileName = "temp_pronunciation.mp3";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                await File.WriteAllBytesAsync(filePath, audioBytes);

                var fileStream = File.OpenRead(filePath);

                _audioPlayer = _audioManager.CreatePlayer(fileStream);
                _audioPlayer.Play();

                _audioPlayer.PlaybackEnded += (s, e) =>
                {
                    fileStream.Dispose();
                    // _audioPlayer.Dispose(); // Można odkomentować, ale ostrożnie przy szybkim klikaniu
                };
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Błąd audio", $"Nie udało się odtworzyć: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Audio Error: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task AddItemToFlashcards(SearchResultItem item)
        {
            if (SelectedCollection == null)
            {
                await Shell.Current.DisplayAlert(
                    AppResources.Error,
                    AppResources.SelectCollectionError,
                    "OK");
                return;
            }

            var flashcard = new Flashcard
            {
                Word = item.Word,
                Translation = item.Translation ?? AppResources.NoTranslation,
                PartOfSpeech = item.PartOfSpeech,
                ExampleSentence = item.Example ?? "",
                CollectionId = SelectedCollection.Id
            };

            try
            {
                await _flashcardService.AddFlashcardAsync(flashcard);

                var message = string.Format(AppResources.AddedToCollectionMessage, SelectedCollection.Name);
                await Shell.Current.DisplayAlert("Sukces", message, "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, ex.Message, "OK");
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
                item.Translation = AppResources.TranslationError;
            }
            finally
            {
                item.IsBusy = false;
            }
        }
    }
}