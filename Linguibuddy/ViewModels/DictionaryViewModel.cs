using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Data;
using Linguibuddy.Helpers;
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

        public DictionaryWord? SourceWordObject { get; set; }

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
        private readonly CollectionService _CollectionService;
        private readonly IAudioManager _audioManager;

        [ObservableProperty] private string? _inputText;

        [ObservableProperty] private bool _isLoading;

        public ObservableCollection<SearchResultItem> SearchResults { get; } = [];

        public ObservableCollection<WordCollection> UserCollections { get; } = [];

        [ObservableProperty] private WordCollection? _selectedCollection;

        private IAudioPlayer? _audioPlayer;

        public DictionaryViewModel(DataContext dataContext,
            DictionaryApiService dictionaryService,
            DeepLTranslationService translationService,
            OpenAiService openAiService,
            CollectionService CollectionService,
            IAudioManager audioManager)
        {
            _dictionaryService = dictionaryService;
            _translationService = translationService;
            _openAiService = openAiService;
            _openAiService = openAiService;
            _CollectionService = CollectionService;
            _audioManager = audioManager;

            LoadCollectionsCommand.Execute(null);
        }

        [RelayCommand]
        public async Task LoadCollections()
        {
            try
            {
                var collections = await _CollectionService.GetUserCollectionsAsync();
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
                                Phonetic = entry.Phonetic,
                                AudioUrl = entry.Audio,
                                PartOfSpeech = meaning.PartOfSpeech,
                                Definition = def.DefinitionText,
                                Example = def.Example,
                                Translation = null,
                                IsBusy = false,

                                SourceWordObject = entry
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

                _audioPlayer.PlaybackEnded += (s, e) => { fileStream.Dispose(); };
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(AppResources.AudioError, AppResources.PlaybackError, "OK");
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

            if (item.SourceWordObject == null)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.NoSourceData, "OK");
                return;
            }

            if (string.IsNullOrEmpty(item.Translation))
            {
                bool translate = await Shell.Current.DisplayAlert(AppResources.TranslationError, AppResources.TranslateBeforeAdding, AppResources.Yes, AppResources.No);
                if (translate)
                {
                    await TranslateItem(item);
                    if (string.IsNullOrEmpty(item.Translation)) return;
                }
                else return;
            }

            var dto = new FlashcardCreationDto
            {
                Word = item.SourceWordObject.Word,
                Phonetic = item.SourceWordObject.Phonetic,
                Audio = item.SourceWordObject.Audio,
                ImageUrl = item.SourceWordObject.ImageUrl,

                PartOfSpeech = item.PartOfSpeech,
                Definition = item.Definition,
                Example = item.Example ?? string.Empty,

                Translation = item.Translation
            };

            try
            {
                await _CollectionService.AddCollectionItemFromDtoAsync(SelectedCollection.Id, dto);

                var message = string.Format(AppResources.AddedToCollectionMessage, SelectedCollection.Name);
                await Shell.Current.DisplayAlert(AppResources.Success, message, "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, ex.Message, "OK");
            }
        }

        [RelayCommand]
        public async Task TranslateItem(SearchResultItem item)
        {
            if (item == null || item.IsBusy || !string.IsNullOrWhiteSpace(item.Translation)) return;

            item.IsBusy = true;

            try
            {
                int apiInt = Preferences.Default.Get(Constants.TranslationApiKey, (int)TranslationProvider.DeepL);
                var provider = (TranslationProvider)apiInt;

                string? translation = null;

                if (provider == TranslationProvider.OpenAi)
                {
                    translation = await _openAiService.TranslateWithContextAsync(
                        item.Word,
                        item.Definition,
                        item.PartOfSpeech
                    );
                }
                else
                {
                    translation = await _translationService.TranslateWithContextAsync(
                        item.Word,
                        item.Definition,
                        item.PartOfSpeech,
                        "PL"
                    );
                }

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