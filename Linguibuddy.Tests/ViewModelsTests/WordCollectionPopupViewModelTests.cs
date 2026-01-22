using CommunityToolkit.Maui;
using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class WordCollectionPopupViewModelTests
{
    private readonly ICollectionService _collectionService;
    private readonly IPopupService _popupService;
    private readonly TestableWordCollectionPopupViewModel _viewModel;

    public WordCollectionPopupViewModelTests()
    {
        _collectionService = A.Fake<ICollectionService>();
        _popupService = A.Fake<IPopupService>();
        _viewModel = new TestableWordCollectionPopupViewModel(_collectionService, _popupService);
    }

    private class TestableWordCollectionPopupViewModel : WordCollectionPopupViewModel
    {
        public WordCollection? LastPopupResult { get; private set; }
        public bool ClosePopupCalled { get; private set; }

        public TestableWordCollectionPopupViewModel(ICollectionService collectionService, IPopupService popupService) 
            : base(collectionService, popupService)
        {
        }

        protected override Task ClosePopup(WordCollection? result)
        {
            ClosePopupCalled = true;
            LastPopupResult = result;
            return Task.CompletedTask;
        }

        protected override void RunInBackground(Func<Task> action)
        {
            
            
        }
    }

    [Fact]
    public async Task LoadCollectionsAsync_ShouldPopulateCollections()
    {
        // Arrange
        var collections = new List<WordCollection> { new() { Id = 1, Name = "Test" } };
        A.CallTo(() => _collectionService.GetUserCollectionsAsync()).Returns(collections);

        // Act
        await _viewModel.LoadCollectionsAsync();

        // Assert
        _viewModel.Collections.Should().HaveCount(1);
        _viewModel.Collections.First().Name.Should().Be("Test");
    }

    [Fact]
    public async Task CollectionSelected_ShouldClosePopupWithResult()
    {
        // Arrange
        var selected = new WordCollection { Id = 1, Name = "Selected" };

        // Act
        await _viewModel.CollectionSelectedCommand.ExecuteAsync(selected);

        // Assert
        _viewModel.ClosePopupCalled.Should().BeTrue();
        _viewModel.LastPopupResult.Should().Be(selected);
    }

    [Fact]
    public async Task Cancel_ShouldClosePopupWithNull()
    {
        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        _viewModel.ClosePopupCalled.Should().BeTrue();
        _viewModel.LastPopupResult.Should().BeNull();
    }
}
