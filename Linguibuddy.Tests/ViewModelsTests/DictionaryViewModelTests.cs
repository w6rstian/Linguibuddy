using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using Plugin.Maui.Audio;

namespace Linguibuddy.Tests.ViewModelsTests;

public class DictionaryViewModelTests
{
    private readonly IAudioManager _audioManager;
    private readonly ICollectionService _collectionService;
    private readonly IDictionaryApiService _dictionaryService;
    private readonly IOpenAiService _openAiService;
    private readonly IDeepLTranslationService _translationService;
    private readonly TestableDictionaryViewModel _viewModel;

    public DictionaryViewModelTests()
    {
        _dictionaryService = A.Fake<IDictionaryApiService>();
        _translationService = A.Fake<IDeepLTranslationService>();
        _openAiService = A.Fake<IOpenAiService>();
        _collectionService = A.Fake<ICollectionService>();
        _audioManager = A.Fake<IAudioManager>();

        _viewModel = new TestableDictionaryViewModel(
            _dictionaryService,
            _translationService,
            _openAiService,
            _collectionService,
            _audioManager);
    }

    [Fact]
    public async Task LoadCollections_ShouldPopulateUserCollections()
    {
        // Arrange
        var collections = new List<WordCollection>
        {
            new() { Id = 1, Name = "Collection 1" },
            new() { Id = 2, Name = "Collection 2" }
        };
        A.CallTo(() => _collectionService.GetUserCollectionsAsync()).Returns(collections);

        // Act
        await _viewModel.LoadCollectionsCommand.ExecuteAsync(null);

        // Assert
        _viewModel.UserCollections.Should().HaveCount(2);
        _viewModel.SelectedCollection.Should().NotBeNull();
        _viewModel.SelectedCollection!.Id.Should().Be(1);
    }

    [Fact]
    public async Task LookupWord_ShouldPopulateSearchResults_WhenWordFound()
    {
        // Arrange
        _viewModel.InputText = "test";
        var dictionaryWord = new DictionaryWord
        {
            Word = "test",
            Meanings = new List<Meaning>
            {
                new()
                {
                    PartOfSpeech = "noun",
                    Definitions = new List<Definition>
                    {
                        new()
                        {
                            DefinitionText =
                                "a procedure intended to establish the quality, performance, or reliability of something."
                        }
                    }
                }
            }
        };
        A.CallTo(() => _dictionaryService.GetEnglishWordAsync("test")).Returns(dictionaryWord);

        // Act
        await _viewModel.LookupWordCommand.ExecuteAsync(null);

        // Assert
        _viewModel.SearchResults.Should().HaveCount(1);
        _viewModel.SearchResults.First().Word.Should().Be("test");
        _viewModel.SearchResults.First().Definition.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LookupWord_ShouldShowAlert_WhenWordNotFound()
    {
        // Arrange
        _viewModel.InputText = "unknownword";
        A.CallTo(() => _dictionaryService.GetEnglishWordAsync("unknownword")).Returns((DictionaryWord?)null);

        // Act
        await _viewModel.LookupWordCommand.ExecuteAsync(null);

        // Assert
        _viewModel.SearchResults.Should().BeEmpty();
        _viewModel.LastAlertMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AddItemToFlashcards_ShouldAddWord_WhenCollectionSelected()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "My Collection" };
        _viewModel.SelectedCollection = collection;

        var searchItem = new SearchResultItem
        {
            Word = "test",
            SourceWordObject = new DictionaryWord { Word = "test" },
            Translation = "test (pl)"
        };

        A.CallTo(() => _collectionService.AddCollectionItemFromDtoAsync(1, A<FlashcardCreationDto>.Ignored))
            .Returns(true);

        // Act
        await _viewModel.AddItemToFlashcardsCommand.ExecuteAsync(searchItem);

        // Assert
        A.CallTo(() => _collectionService.AddCollectionItemFromDtoAsync(1,
                A<FlashcardCreationDto>.That.Matches(d => d.Word == "test" && d.Translation == "test (pl)")))
            .MustHaveHappenedOnceExactly();
        _viewModel.LastAlertMessage.Should().Contain("My Collection");
    }

    [Fact]
    public async Task AddItemToFlashcards_ShouldShowError_WhenNoCollectionSelected()
    {
        // Arrange
        _viewModel.SelectedCollection = null;
        var searchItem = new SearchResultItem { Word = "test" };

        // Act
        await _viewModel.AddItemToFlashcardsCommand.ExecuteAsync(searchItem);

        // Assert
        _viewModel.LastAlertMessage.Should().NotBeNullOrEmpty();
        A.CallTo(() =>
                _collectionService.AddCollectionItemFromDtoAsync(A<int>.Ignored, A<FlashcardCreationDto>.Ignored))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task TranslateItem_ShouldUseDeepL_WhenConfigured()
    {
        // Arrange
        _viewModel.MockTranslationApi = (int)TranslationProvider.DeepL;
        var item = new SearchResultItem { Word = "hello", Definition = "greeting", PartOfSpeech = "noun" };
        A.CallTo(() => _translationService.TranslateWithContextAsync("hello", "greeting", "noun", "PL"))
            .Returns("cześć");

        // Act
        await _viewModel.TranslateItemCommand.ExecuteAsync(item);

        // Assert
        item.Translation.Should().Be("cześć");
        A.CallTo(() =>
                _openAiService.TranslateWithContextAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task TranslateItem_ShouldUseOpenAi_WhenConfigured()
    {
        // Arrange
        _viewModel.MockTranslationApi = (int)TranslationProvider.OpenAi;
        var item = new SearchResultItem { Word = "hello", Definition = "greeting", PartOfSpeech = "noun" };
        A.CallTo(() => _openAiService.TranslateWithContextAsync("hello", "greeting", "noun")).Returns("witaj");

        // Act
        await _viewModel.TranslateItemCommand.ExecuteAsync(item);

        // Assert
        item.Translation.Should().Be("witaj");

        A.CallTo(() =>
            _translationService.TranslateWithContextAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored,
                A<string>.Ignored)).MustNotHaveHappened();
    }

    private class TestableDictionaryViewModel : DictionaryViewModel
    {
        public TestableDictionaryViewModel(
            IDictionaryApiService dictionaryService,
            IDeepLTranslationService translationService,
            IOpenAiService openAiService,
            ICollectionService collectionService,
            IAudioManager audioManager)
            : base(dictionaryService, translationService, openAiService, collectionService, audioManager)
        {
        }

        public bool MockNetworkStatus { get; } = true;
        public string? LastAlertMessage { get; private set; }
        public int MockTranslationApi { get; set; } = (int)TranslationProvider.DeepL;

        protected override bool IsNetworkConnected()
        {
            return MockNetworkStatus;
        }

        protected override Task ShowAlertAsync(string title, string message, string cancel)
        {
            LastAlertMessage = message;
            return Task.CompletedTask;
        }

        protected override string GetCacheDirectory()
        {
            return Path.GetTempPath();
        }

        protected override int GetTranslationApiPreference()
        {
            return MockTranslationApi;
        }
    }
}