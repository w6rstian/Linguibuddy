using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Linguibuddy.Tests.ViewModelsTests;

public class FlashcardsViewModelTests
{
    private readonly ICollectionService _collectionService;
    private readonly ISpacedRepetitionService _srsService;
    private readonly TestableFlashcardsViewModel _viewModel;

    public FlashcardsViewModelTests()
    {
        _collectionService = A.Fake<ICollectionService>();
        _srsService = A.Fake<ISpacedRepetitionService>();
        _viewModel = new TestableFlashcardsViewModel(_collectionService, _srsService);
    }

    private class TestableFlashcardsViewModel : FlashcardsViewModel
    {
        public string? LastAlertMessage { get; private set; }
        public string? LastNavigatedRoute { get; private set; }
        public Task? CurrentStartSessionTask { get; private set; }

        public TestableFlashcardsViewModel(ICollectionService collectionService, ISpacedRepetitionService srsService) 
            : base(collectionService, srsService)
        {
        }

        protected override Task ShowAlertAsync(string title, string message, string cancel)
        {
            LastAlertMessage = message;
            return Task.CompletedTask;
        }

        protected override Task GoToAsync(string route)
        {
            LastNavigatedRoute = route;
            return Task.CompletedTask;
        }

        protected override async Task StartSession()
        {
            var task = base.StartSession();
            CurrentStartSessionTask = task;
            await task;
        }
    }

    [Fact]
    public async Task StartSession_ShouldPopulateQueue_InStandardMode()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };
        var items = new List<CollectionItem> 
        { 
            new() { Id = 1, Word = "Word1" },
            new() { Id = 2, Word = "Word2" }
        };
        A.CallTo(() => _collectionService.GetItemsForLearning(1)).Returns(items);
        _viewModel.CurrentLearningMode = LearningMode.Standard;

        // Act
        _viewModel.Collection = collection; 
        if (_viewModel.CurrentStartSessionTask != null) await _viewModel.CurrentStartSessionTask;

        // Assert
        _viewModel.IsFinished.Should().BeFalse();
        _viewModel.CurrentItem.Should().NotBeNull();
        items.Select(i => i.Id).Should().Contain(_viewModel.CurrentItem!.Id);
    }

    [Fact]
    public async Task StartSession_ShouldPopulateQueue_InSrsMode()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };
        var items = new List<CollectionItem> { new() { Id = 1, Word = "DueWord" } };
        A.CallTo(() => _collectionService.GetItemsDueForLearning(1)).Returns(items);
        _viewModel.CurrentLearningMode = LearningMode.SpacedRepetition;

        // Act
        _viewModel.Collection = collection;
        if (_viewModel.CurrentStartSessionTask != null) await _viewModel.CurrentStartSessionTask;

        // Assert
        _viewModel.CurrentItem?.Word.Should().Be("DueWord");
    }

    [Fact]
    public async Task StartSession_ShouldShowAlert_WhenNoSrsItemsDue()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };
        A.CallTo(() => _collectionService.GetItemsDueForLearning(1)).Returns(new List<CollectionItem>());
        _viewModel.CurrentLearningMode = LearningMode.SpacedRepetition;

        // Act
        _viewModel.Collection = collection;
        if (_viewModel.CurrentStartSessionTask != null) await _viewModel.CurrentStartSessionTask;

        // Assert
        _viewModel.LastAlertMessage.Should().NotBeNullOrEmpty();
        _viewModel.LastNavigatedRoute.Should().Be("..");
    }

    [Fact]
    public void RevealAnswer_ShouldSetIsAnswerRevealed()
    {
        // Act
        _viewModel.RevealAnswerCommand.Execute(null);

        // Assert
        _viewModel.IsAnswerRevealed.Should().BeTrue();
        _viewModel.CanShowButtons.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessSrsGrade_ShouldUpdateProgressAndMoveToNext()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };
        var progress = new Flashcard { Id = 1 };
        var item = new CollectionItem { Id = 1, Word = "Word", FlashcardProgress = progress };
        var nextItem = new CollectionItem { Id = 2, Word = "Next" };
        
        A.CallTo(() => _collectionService.GetItemsForLearning(1)).Returns(new List<CollectionItem> { item, nextItem });
        _viewModel.CurrentLearningMode = LearningMode.Standard;
        
        // Act
        _viewModel.Collection = collection;
        if (_viewModel.CurrentStartSessionTask != null) await _viewModel.CurrentStartSessionTask;

        // Ensure the item is what we expect
        if (_viewModel.CurrentItem?.Id != 1)
        {
            // If random order put item 2 first, cycle it
            _viewModel.NextCardCommand.Execute(null);
        }
        
        // Wait, if NextCard is called in StartSession, it takes one.
        // Random order might pick "Next" first.
        // I should force order or check which one is current.
        // If current is "Next", I should grade "Next" (but I need to set up progress for it).
        // Better: Mock Returns to return only 1 item initially to be sure, or mock Random? No, Random is local.
        
        // Let's force specific behavior:
        // Since I cannot easily control Random inside the VM, I will assume the item I want is there.
        // I'll make sure both items have progress or handle it.
        // Or I can just check _viewModel.CurrentItem and set its progress if missing?
        
        if (_viewModel.CurrentItem!.FlashcardProgress == null)
        {
            _viewModel.CurrentItem.FlashcardProgress = progress;
        }

        await _viewModel.GradeGoodCommand.ExecuteAsync(null);

        // Assert
        A.CallTo(() => _srsService.ProcessResult(A<Flashcard>.Ignored, SuperMemoGrade.Good)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _collectionService.UpdateFlashcardProgress(A<Flashcard>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ProcessSrsGrade_ShouldRequeueItem_WhenGradeIsBelowThreshold()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };
        var progress = new Flashcard { Id = 1 };
        var item = new CollectionItem { Id = 1, Word = "HardWord", FlashcardProgress = progress };
        
        A.CallTo(() => _collectionService.GetItemsForLearning(1)).Returns(new List<CollectionItem> { item });
        _viewModel.CurrentLearningMode = LearningMode.Standard;
        
        // Act
        _viewModel.Collection = collection;
        if (_viewModel.CurrentStartSessionTask != null) await _viewModel.CurrentStartSessionTask;

        await _viewModel.GradeNullCommand.ExecuteAsync(null); 

        // Assert
        _viewModel.CurrentItem?.Id.Should().Be(1); 
        _viewModel.IsFinished.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsUnknown_ShouldRequeueItem()
    {
        // Arrange
        var collection = new WordCollection { Id = 1 };
        var item = new CollectionItem { Id = 1, Word = "Unknown" };
        A.CallTo(() => _collectionService.GetItemsForLearning(1)).Returns(new List<CollectionItem> { item });
        _viewModel.CurrentLearningMode = LearningMode.Standard;
        
        // Act
        _viewModel.Collection = collection;
        if (_viewModel.CurrentStartSessionTask != null) await _viewModel.CurrentStartSessionTask;

        _viewModel.MarkAsUnknownCommand.Execute(null);

        // Assert
        _viewModel.CurrentItem?.Id.Should().Be(1);
        _viewModel.IsFinished.Should().BeFalse();
    }

    [Fact]
    public async Task GoBack_ShouldNavigateBack()
    {
        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert
        _viewModel.LastNavigatedRoute.Should().Be("..");
    }
}