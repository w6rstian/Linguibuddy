using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class CollectionDetailsViewModelTests
{
    private readonly ICollectionService _collectionService;
    private readonly IOpenAiService _openAiService;
    private readonly IAppUserService _appUserService;
    private readonly TestableCollectionDetailsViewModel _viewModel;

    public CollectionDetailsViewModelTests()
    {
        _collectionService = A.Fake<ICollectionService>();
        _openAiService = A.Fake<IOpenAiService>();
        _appUserService = A.Fake<IAppUserService>();
        _viewModel = new TestableCollectionDetailsViewModel(_collectionService, _openAiService, _appUserService);
    }

    private class TestableCollectionDetailsViewModel : CollectionDetailsViewModel
    {
        public string? MockPromptResult { get; set; }
        public bool MockAlertResult { get; set; } = true;
        public string MockPreference { get; set; } = "en";
        public string? LastNavigatedRoute { get; private set; }
        public IDictionary<string, object>? LastNavigatedParameters { get; private set; }

        public TestableCollectionDetailsViewModel(ICollectionService collectionService, IOpenAiService openAiService, IAppUserService appUserService)
            : base(collectionService, openAiService, appUserService)
        {
        }

        protected override string GetPreference(string key, string defaultValue) => MockPreference;

        protected override Task GoToAsync(string route, IDictionary<string, object> parameters)
        {
            LastNavigatedRoute = route;
            LastNavigatedParameters = parameters;
            return Task.CompletedTask;
        }

        protected override Task<string> ShowPromptAsync(string title, string message, string accept, string cancel, string initialValue)
        {
            return Task.FromResult(MockPromptResult ?? string.Empty);
        }

        protected override Task<bool> ShowAlertAsync(string title, string message, string accept, string cancel)
        {
            return Task.FromResult(MockAlertResult);
        }
    }

    [Fact]
    public async Task LoadDataAsync_ShouldPopulateItems_WhenCollectionIsSet()
    {
        // Arrange
        var collection = new WordCollection
        {
            Id = 1,
            Name = "Test Collection",
            Items = new List<CollectionItem>
            {
                new() { Id = 1, Word = "Word1" },
                new() { Id = 2, Word = "Word2" }
            }
        };
        _viewModel.Collection = collection;
        A.CallTo(() => _collectionService.GetCollection(1)).Returns(collection);

        // Act
        await _viewModel.LoadDataCommand.ExecuteAsync(null);

        // Assert
        _viewModel.Items.Should().HaveCount(2);
        _viewModel.Items[0].Word.Should().Be("Word1");
        _viewModel.Items[1].Word.Should().Be("Word2");
    }

    [Fact]
    public async Task LoadAiFeedback_ShouldCallOpenAi_WhenAnalysisIsRequired()
    {
        // Arrange
        var collection = new WordCollection
        {
            Id = 1,
            RequiresAiAnalysis = true,
            Items = new List<CollectionItem>()
        };
        _viewModel.Collection = collection;
        
        A.CallTo(() => _collectionService.GetCollection(A<int>.Ignored)).Returns(collection);
        A.CallTo(() => _appUserService.GetUserDifficultyAsync()).Returns(DifficultyLevel.A1);
        A.CallTo(() => _openAiService.AnalyzeCollectionProgressAsync(A<WordCollection>.Ignored, A<DifficultyLevel>.Ignored, A<string>.Ignored))
            .Returns(Task.FromResult("AI Feedback"));

        // Act
        await _viewModel.LoadDataCommand.ExecuteAsync(null);

        // Assert
        _viewModel.AiFeedback.Should().Be("AI Feedback");
        collection.RequiresAiAnalysis.Should().BeFalse();
        A.CallTo(() => _collectionService.UpdateCollectionAsync(A<WordCollection>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddToCollection_ShouldNavigateToDictionaryPage()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Test" };
        _viewModel.Collection = collection;

        // Act
        await _viewModel.AddToCollectionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be("///DictionaryPage");
        _viewModel.LastNavigatedParameters.Should().ContainKey("TargetCollection");
        _viewModel.LastNavigatedParameters?["TargetCollection"].Should().Be(collection);
    }

    [Fact]
    public async Task RenameCollection_ShouldUpdateName_WhenPromptIsConfirmed()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Old Name" };
        _viewModel.Collection = collection;
        _viewModel.MockPromptResult = "New Name";

        // Act
        await _viewModel.RenameCollectionCommand.ExecuteAsync(null);

        // Assert
        A.CallTo(() => _collectionService.RenameCollectionAsync(collection, "New Name")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task DeleteItem_ShouldRemoveItem_WhenConfirmed()
    {
        // Arrange
        var item = new CollectionItem { Id = 1, Word = "DeleteMe" };
        var collection = new WordCollection
        {
            Id = 1,
            Items = new List<CollectionItem> { item }
        };
        _viewModel.Collection = collection;
        _viewModel.Items.Add(item);
        _viewModel.MockAlertResult = true;

        // Act
        await _viewModel.DeleteItemCommand.ExecuteAsync(item);

        // Assert
        _viewModel.Items.Should().BeEmpty();
        collection.Items.Should().BeEmpty();
        A.CallTo(() => _collectionService.DeleteCollectionItemAsync(item)).MustHaveHappenedOnceExactly();
    }
}
