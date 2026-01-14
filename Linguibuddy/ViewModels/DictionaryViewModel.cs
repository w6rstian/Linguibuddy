using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Data;
using Linguibuddy.Helpers;
using Linguibuddy.Models;
using Linguibuddy.Resources.Strings;
using Linguibuddy.Services;
using Plugin.Maui.Audio;

namespace Linguibuddy.ViewModels;

public partial class SearchResultItem : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowTranslateButton))]
    private bool _isBusy;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(ShowTranslateButton))]
    private string? _translation;

    public string Word { get; set; } = string.Empty;
    public string PartOfSpeech { get; set; } = string.Empty;
    public string Definition { get; set; } = string.Empty;
    public string? Example { get; set; }
    public string Phonetic { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }

    public DictionaryWord? SourceWordObject { get; set; }

    public bool ShowTranslateButton => string.IsNullOrEmpty(Translation) && !IsBusy;
}

public partial class DictionaryViewModel : ObservableObject
{
    private readonly IAudioManager _audioManager;
    private readonly CollectionService _CollectionService;
    private readonly DictionaryApiService _dictionaryService;
    private readonly OpenAiService _openAiService;
    private readonly DeepLTranslationService _translationService;

    private IAudioPlayer? _audioPlayer;

    [ObservableProperty] private string? _inputText;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private WordCollection? _selectedCollection;

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
    }

    public ObservableCollection<SearchResultItem> SearchResults { get; } = [];

    public ObservableCollection<WordCollection> UserCollections { get; } = [];

    [RelayCommand]
    public async Task LoadCollections()
    {
        try
        {
            var collections = await _CollectionService.GetUserCollectionsAsync();
            UserCollections.Clear();
            foreach (var col in collections) UserCollections.Add(col);

            if (UserCollections.Any())
                SelectedCollection = UserCollections.First();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Błąd ładowania kolekcji: {ex.Message}");
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
                foreach (var def in meaning.Definitions)
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
            else
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await Shell.Current.DisplayAlert(AppResources.NetworkError, AppResources.NetworkRequired, "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert(AppResources.Dictionary, AppResources.NoResultsFoundText, "OK");
                }
            }
                
        }
        catch (Exception ex)
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await Shell.Current.DisplayAlert(AppResources.NetworkError, AppResources.NetworkRequired, "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FailedWordRetrieval, "OK");
            }

            Debug.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task PlayAudio(SearchResultItem item)
    {
        if (item == null) return;

        if (!string.IsNullOrWhiteSpace(item.AudioUrl))
        {
            if (_audioPlayer != null && _audioPlayer.IsPlaying) _audioPlayer.Dispose();

            try
            {
                using var client = new HttpClient();
                var audioBytes = await client.GetByteArrayAsync(item.AudioUrl);
                var fileName = "temp_pronunciation.mp3";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                await File.WriteAllBytesAsync(filePath, audioBytes);
                var fileStream = File.OpenRead(filePath);

                _audioPlayer = _audioManager.CreatePlayer(fileStream);
                _audioPlayer.Play();
                _audioPlayer.PlaybackEnded += (s, e) => { fileStream.Dispose(); };
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio file failed, falling back to TTS. Error: {ex.Message}");
            }
        }

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            string[] femaleVoices = { "Zira", "Paulina", "Jenny", "Aria" };
            // preferowany język US i GB na Android i Windows
            var preferred = locales.FirstOrDefault(l =>
                                (l.Language == "en-US" || (l.Language == "en" && l.Country == "US")) &&
                                femaleVoices.Any(f => l.Name.Contains(f)))
                            ?? locales.FirstOrDefault(l =>
                                (l.Language == "en-GB" || (l.Language == "en" && l.Country == "GB")) &&
                                femaleVoices.Any(f => l.Name.Contains(f)))
                            ?? locales.FirstOrDefault(l =>
                                l.Language.StartsWith("en") && femaleVoices.Any(f => l.Name.Contains(f)))
                            // inne głosy
                            ?? locales.FirstOrDefault(l =>
                                l.Language == "en-US" || (l.Language == "en" && l.Country == "US"))
                            ?? locales.FirstOrDefault(l =>
                                l.Language == "en-GB" || (l.Language == "en" && l.Country == "GB"))
                            ?? locales.FirstOrDefault(l => l.Language.StartsWith("en"));

            if (preferred == null)
            {
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.InstallEng, "OK");
                return;
            }

            await TextToSpeech.Default.SpeakAsync(item.Word, new SpeechOptions
            {
                Locale = preferred,
                Pitch = 1.0f,
                Volume = 1.0f
            });
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert(AppResources.AudioError, AppResources.PlaybackError, "OK");
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
            var translate = await Shell.Current.DisplayAlert(AppResources.TranslationError,
                AppResources.TranslateBeforeAdding, AppResources.Yes, AppResources.No);
            if (translate)
            {
                await TranslateItem(item);
                if (string.IsNullOrEmpty(item.Translation)) return;
            }
            else
            {
                return;
            }
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
            var apiInt = Preferences.Default.Get(Constants.TranslationApiKey, (int)TranslationProvider.DeepL);
            var provider = (TranslationProvider)apiInt;

            string? translation = null;

            if (provider == TranslationProvider.OpenAi)
                translation = await _openAiService.TranslateWithContextAsync(
                    item.Word,
                    item.Definition,
                    item.PartOfSpeech
                );
            else
                translation = await _translationService.TranslateWithContextAsync(
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