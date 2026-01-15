using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
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

public class MiniGamesViewModelTests
{
    private readonly ICollectionService _collectionService;
    private readonly IPopupService _popupService;
    private readonly TestableMiniGamesViewModel _viewModel;

    public MiniGamesViewModelTests()
    {
        _collectionService = A.Fake<ICollectionService>();
        _popupService = A.Fake<IPopupService>();
        _viewModel = new TestableMiniGamesViewModel(_collectionService, _popupService);
    }

    private class TestableMiniGamesViewModel : MiniGamesViewModel
    {
        public WordCollection? MockSelectedCollection { get; set; }
        public string LastNavigatedRoute { get; private set; } = string.Empty;
        public IDictionary<string, object>? LastNavigatedParameters { get; private set; }
        public bool AlertShown { get; private set; }

        public TestableMiniGamesViewModel(ICollectionService collectionService, IPopupService popupService) 
            : base(collectionService, popupService)
        {
        }

        protected override Task<WordCollection?> GetSelectedCollectionFromPopupAsync()
        {
            return Task.FromResult(MockSelectedCollection);
        }

        protected override Task ShowAlertAsync(string title, string message, string cancel)
        {
            AlertShown = true;
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
    public async Task NavigateToAudioQuiz_ShouldNavigate_WhenCollectionIsSelectedAndNotEmpty()
    {
        // Arrange
        var collection = new WordCollection 
        { 
            Id = 1, 
            Items = new List<CollectionItem> { new CollectionItem { Word = "Test" } } 
        };
        _viewModel.MockSelectedCollection = collection;

        // Act
        await _viewModel.NavigateToAudioQuizCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be(nameof(AudioQuizPage));
        _viewModel.LastNavigatedParameters.Should().ContainKey("SelectedCollection");
        _viewModel.LastNavigatedParameters?["SelectedCollection"].Should().Be(collection);
    }

    [Fact]
    public async Task NavigateToImageQuiz_ShouldNavigate_WhenCollectionIsSelectedAndNotEmpty()
    {
        // Arrange
        var collection = new WordCollection 
        { 
            Id = 1, 
            Items = new List<CollectionItem> { new CollectionItem { Word = "Test" } } 
        };
        _viewModel.MockSelectedCollection = collection;

        // Act
        await _viewModel.NavigateToImageQuizCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be(nameof(ImageQuizPage));
    }

    [Fact]
    public async Task NavigateToHangman_ShouldNavigate_WhenCollectionIsSelectedAndNotEmpty()
    {
        // Arrange
        var collection = new WordCollection 
        { 
            Id = 1, 
            Items = new List<CollectionItem> { new CollectionItem { Word = "Test" } } 
        };
        _viewModel.MockSelectedCollection = collection;

        // Act
        await _viewModel.NavigateToHangmanCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be(nameof(HangmanPage));
    }

    [Fact]
    public async Task NavigateToGame_ShouldNotNavigate_WhenPopupIsCancelled()
    {
        // Arrange
        _viewModel.MockSelectedCollection = null;

        // Act
        await _viewModel.NavigateToAudioQuizCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().BeEmpty();
    }

    [Fact]
    public async Task NavigateToGame_ShouldShowAlert_WhenCollectionIsEmpty()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Items = new List<CollectionItem>() };
        _viewModel.MockSelectedCollection = collection;

        // Act
        await _viewModel.NavigateToAudioQuizCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().BeEmpty();
        _viewModel.AlertShown.Should().BeTrue();
    }
}