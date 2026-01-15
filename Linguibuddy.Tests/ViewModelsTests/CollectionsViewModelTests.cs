using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using Linguibuddy.Views;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class CollectionsViewModelTests
{
    private readonly ICollectionService _collectionService;
    private readonly TestableCollectionsViewModel _viewModel;

    public CollectionsViewModelTests()
    {
        _collectionService = A.Fake<ICollectionService>();
        _viewModel = new TestableCollectionsViewModel(_collectionService);
    }

    // Subclass to override protected virtual methods
    private class TestableCollectionsViewModel : CollectionsViewModel
    {
        public string PromptResult { get; set; } = string.Empty;
        public bool AlertResult { get; set; } = true;
        
        public string LastNavigatedRoute { get; private set; } = string.Empty;
        public IDictionary<string, object>? LastNavigatedParameters { get; private set; }

        public string LastAlertTitle { get; private set; } = string.Empty;
        public string LastAlertMessage { get; private set; } = string.Empty;

        public TestableCollectionsViewModel(ICollectionService collectionService) : base(collectionService) { }

        protected override Task<string> ShowPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
        {
            return Task.FromResult(PromptResult);
        }

        protected override Task<bool> ShowAlertAsync(string title, string message, string accept, string cancel)
        {
            LastAlertTitle = title;
            LastAlertMessage = message;
            return Task.FromResult(AlertResult);
        }

        protected override Task ShowAlertAsync(string title, string message, string cancel)
        {
            LastAlertTitle = title;
            LastAlertMessage = message;
            return Task.CompletedTask;
        }

        protected override Task GoToAsync(string route, IDictionary<string, object> parameters)
        {
            LastNavigatedRoute = route;
            LastNavigatedParameters = parameters;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task LoadCollections_ShouldPopulateCollections_WhenServiceReturnsData()
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
        _viewModel.Collections.Should().HaveCount(2);
        _viewModel.Collections.Should().BeEquivalentTo(collections);
    }

    [Fact]
    public async Task CreateCollection_ShouldCreateAndReload_WhenNameIsProvided()
    {
        // Arrange
        _viewModel.PromptResult = "New Collection";
        A.CallTo(() => _collectionService.GetUserCollectionsAsync()).Returns(new List<WordCollection>());

        // Act
        await _viewModel.CreateCollectionCommand.ExecuteAsync(null);

        // Assert
        A.CallTo(() => _collectionService.CreateCollectionAsync("New Collection")).MustHaveHappenedOnceExactly();
        A.CallTo(() => _collectionService.GetUserCollectionsAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task CreateCollection_ShouldDoNothing_WhenNameIsEmpty()
    {
        // Arrange
        _viewModel.PromptResult = "";

        // Act
        await _viewModel.CreateCollectionCommand.ExecuteAsync(null);

        // Assert
        A.CallTo(() => _collectionService.CreateCollectionAsync(A<string>.Ignored)).MustNotHaveHappened();
    }

    [Fact]
    public async Task EditCollection_ShouldNavigateToDetails_WhenCollectionIsValid()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Test" };

        // Act
        await _viewModel.EditCollectionCommand.ExecuteAsync(collection);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be(nameof(CollectionDetailsPage));
        _viewModel.LastNavigatedParameters.Should().ContainKey("Collection");
        _viewModel.LastNavigatedParameters?["Collection"].Should().Be(collection);
    }

    [Fact]
    public async Task DeleteCollection_ShouldDeleteAndReload_WhenConfirmed()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Delete Me" };
        _viewModel.AlertResult = true; // User clicks "Yes"

        // Act
        await _viewModel.DeleteCollectionCommand.ExecuteAsync(collection);

        // Assert
        A.CallTo(() => _collectionService.DeleteCollectionAsync(collection)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _collectionService.GetUserCollectionsAsync()).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteCollection_ShouldDoNothing_WhenNotConfirmed()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Safe" };
        _viewModel.AlertResult = false; // User clicks "No"

        // Act
        await _viewModel.DeleteCollectionCommand.ExecuteAsync(collection);

        // Assert
        A.CallTo(() => _collectionService.DeleteCollectionAsync(A<WordCollection>.Ignored)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GoToLearning_ShouldNavigateToFlashcards_WithStandardMode_WhenSRSDisabled()
    {
        // Arrange
        var collection = new WordCollection 
        { 
            Id = 1, 
            Items = new List<CollectionItem> { new CollectionItem { Word = "Word" } } 
        };
        _viewModel.IsSpacedRepetitionEnabled = false;

        // Act
        await _viewModel.GoToLearningCommand.ExecuteAsync(collection);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be(nameof(FlashcardsPage));
        _viewModel.LastNavigatedParameters.Should().Contain("Mode", LearningMode.Standard);
        _viewModel.LastNavigatedParameters.Should().Contain("Collection", collection);
    }

    [Fact]
    public async Task GoToLearning_ShouldNavigateToFlashcards_WithSRSMode_WhenSRSEnabled()
    {
        // Arrange
        var collection = new WordCollection 
        { 
            Id = 1, 
            Items = new List<CollectionItem> { new CollectionItem { Word = "Word" } } 
        };
        _viewModel.IsSpacedRepetitionEnabled = true;

        // Act
        await _viewModel.GoToLearningCommand.ExecuteAsync(collection);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be(nameof(FlashcardsPage));
        _viewModel.LastNavigatedParameters.Should().Contain("Mode", LearningMode.SpacedRepetition);
        _viewModel.LastNavigatedParameters.Should().Contain("Collection", collection);
    }

    [Fact]
    public async Task GoToLearning_ShouldShowAlert_WhenCollectionIsEmpty()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Items = new List<CollectionItem>() };

        // Act
        await _viewModel.GoToLearningCommand.ExecuteAsync(collection);

        // Assert
        _viewModel.LastNavigatedRoute.Should().BeEmpty();
        _viewModel.LastAlertMessage.Should().NotBeEmpty(); // Check if alert was shown
    }
}
