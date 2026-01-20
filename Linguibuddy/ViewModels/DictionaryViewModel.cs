using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linguibuddy.Data;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
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

[QueryProperty(nameof(TargetCollection), "TargetCollection")]
public partial class DictionaryViewModel : ObservableObject
{
    private readonly IAudioManager _audioManager;
    private readonly ICollectionService _collectionService;
    private readonly IDictionaryApiService _dictionaryService;
    private readonly IOpenAiService _openAiService;
    private readonly IDeepLTranslationService _translationService;

    private IAudioPlayer? _audioPlayer;

    [ObservableProperty] private string? _inputText;

    [ObservableProperty] private bool _isLoading;

    [ObservableProperty] private WordCollection? _selectedCollection;

    [ObservableProperty] private WordCollection? _targetCollection;

    partial void OnTargetCollectionChanged(WordCollection? value)
    {
        if (value != null)
        {
            SelectedCollection = value;
        }
    }

    public DictionaryViewModel(
        IDictionaryApiService dictionaryService,
        IDeepLTranslationService translationService,
        IOpenAiService openAiService,
        ICollectionService collectionService,
        IAudioManager audioManager)
    {
        _dictionaryService = dictionaryService;
        _translationService = translationService;
        _openAiService = openAiService;
        _collectionService = collectionService;
        _audioManager = audioManager;
    }

    public ObservableCollection<SearchResultItem> SearchResults { get; } = [];

    public ObservableCollection<WordCollection> UserCollections { get; } = [];

    [RelayCommand]
    public async Task LoadCollections()
    {
        try
        {
            var collections = await _collectionService.GetUserCollectionsAsync();
            UserCollections.Clear();
            foreach (var col in collections) UserCollections.Add(col);

            if (TargetCollection != null)
            {
                SelectedCollection = UserCollections.FirstOrDefault(c => c.Id == TargetCollection.Id);
            }
            else if (UserCollections.Any())
            {
                SelectedCollection = UserCollections.First();
            }
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
                if (!IsNetworkConnected())
                {
                    await ShowAlertAsync(AppResources.NetworkError, AppResources.NetworkRequired, "OK");
                }
                else
                {
                    await ShowAlertAsync(AppResources.Dictionary, AppResources.NoResultsFoundText, "OK");
                }
            }
                
        }
        catch (Exception ex)
        {
            if (!IsNetworkConnected())
            {
                await ShowAlertAsync(AppResources.NetworkError, AppResources.NetworkRequired, "OK");
            }
            else
            {
                await ShowAlertAsync(AppResources.Error, AppResources.FailedWordRetrieval, "OK");
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

        var url = item.AudioUrl;
        var wordToSpeak = item.Word;
        bool playedSuccessfully = false;

        if (!string.IsNullOrWhiteSpace(url))
        {
            if (_audioPlayer != null && _audioPlayer.IsPlaying) _audioPlayer.Dispose();

            try
            {
                using var client = new HttpClient();
                var audioBytes = await client.GetByteArrayAsync(url);
                var fileName = "temp_pronunciation.mp3";
                var filePath = Path.Combine(GetCacheDirectory(), fileName);
                await File.WriteAllBytesAsync(filePath, audioBytes);
                var fileStream = File.OpenRead(filePath);

                _audioPlayer = _audioManager.CreatePlayer(fileStream);
                _audioPlayer.Play();
                _audioPlayer.PlaybackEnded += (s, e) => { fileStream.Dispose(); };
                playedSuccessfully = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Audio file failed, falling back to TTS. Error: {ex.Message}");
            }
        }

        if (!playedSuccessfully)
        {
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
                    await ShowAlertAsync(AppResources.Error, AppResources.InstallEng, "OK");
                    return;
                }

                await TextToSpeech.Default.SpeakAsync(wordToSpeak, new SpeechOptions
                {
                    Locale = preferred,
                    Pitch = 1.0f,
                    Volume = 1.0f
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TTS failed: {ex.Message}");
                await ShowAlertAsync(AppResources.AudioError, AppResources.PlaybackError, "OK");
            }
        }
    }

    [RelayCommand]
    public async Task AddItemToFlashcards(SearchResultItem item)
    {
        if (SelectedCollection == null)
        {
            await ShowAlertAsync(
                AppResources.Error,
                AppResources.SelectCollectionError,
                "OK");
            return;
        }

        if (item.SourceWordObject == null)
        {
            await ShowAlertAsync(AppResources.Error, AppResources.NoSourceData, "OK");
            return;
        }

        if (string.IsNullOrEmpty(item.Translation))
        {
            //var translate = await Shell.Current.DisplayAlert(AppResources.TranslationError, AppResources.TranslateBeforeAdding, AppResources.Yes, AppResources.No);

            await TranslateItem(item);

            if (string.IsNullOrEmpty(item.Translation) || item.Translation == AppResources.TranslationError)
            {
                await ShowAlertAsync(AppResources.Error, AppResources.TranslationError, "OK");
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
            bool isAdded = await _collectionService.AddCollectionItemFromDtoAsync(SelectedCollection.Id, dto);

            if (isAdded)
            {
                var message = string.Format(AppResources.AddedToCollectionMessage, SelectedCollection.Name);
                await ShowAlertAsync(AppResources.Success, message, "OK");
            }

            else
            {
                await ShowAlertAsync(
                    AppResources.Success,
                    AppResources.ItemExists,
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowAlertAsync(AppResources.Error, ex.Message, "OK");
        }
    }

    [RelayCommand]
    public async Task TranslateItem(SearchResultItem item)
    {
        if (item == null || item.IsBusy || !string.IsNullOrWhiteSpace(item.Translation)) return;

        item.IsBusy = true;

        try
        {
            var apiInt = GetTranslationApiPreference();
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

    protected virtual bool IsNetworkConnected()
    {
        return Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
    }

    protected virtual Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlert(title, message, cancel);
    }

    protected virtual string GetCacheDirectory()
    {
        return FileSystem.CacheDirectory;
    }

    protected virtual int GetTranslationApiPreference()
    {
        return Preferences.Default.Get(Constants.TranslationApiKey, (int)TranslationProvider.DeepL);
    }
}