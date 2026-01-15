using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;
using Microsoft.EntityFrameworkCore;

namespace Linguibuddy.Tests.ServicesTests;

public class CollectionServiceTests : IDisposable
{
    private readonly IAuthService _auth;
    private readonly DataContext _db;
    private readonly CollectionService _sut;
    private readonly string _userId = "user123";

    public CollectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new DataContext(options);
        _auth = A.Fake<IAuthService>();

        A.CallTo(() => _auth.CurrentUserId).Returns(_userId);

        _sut = new CollectionService(_db, _auth);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GetUserCollectionsAsync_ShouldReturnCollections_WhenUserHasCollections()
    {
        // Arrange
        var collection1 = new WordCollection { Id = 1, Name = "Col1", UserId = _userId };
        var collection2 = new WordCollection { Id = 2, Name = "Col2", UserId = _userId };
        var otherCollection = new WordCollection { Id = 3, Name = "OtherCol", UserId = "otherUser" };

        _db.WordCollections.AddRange(collection1, collection2, otherCollection);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetUserCollectionsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "Col1");
        result.Should().Contain(c => c.Name == "Col2");
    }

    [Fact]
    public async Task GetUserCollectionsAsync_ShouldThrowUnauthorizedAccessException_WhenUserNotLoggedIn()
    {
        // Arrange
        A.CallTo(() => _auth.CurrentUserId).Returns(string.Empty);

        // Act
        var act = async () => await _sut.GetUserCollectionsAsync();

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Użytkownik nie jest zalogowany.");
    }

    [Fact]
    public async Task CreateCollectionAsync_ShouldAddCollectionToDb()
    {
        // Arrange
        var collectionName = "New Collection";

        // Act
        await _sut.CreateCollectionAsync(collectionName);

        // Assert
        var collection = await _db.WordCollections.FirstOrDefaultAsync(c => c.Name == collectionName);
        collection.Should().NotBeNull();
        collection!.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task GetCollection_ShouldReturnCollection_WhenExists()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Test Col", UserId = _userId };
        _db.WordCollections.Add(collection);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetCollection(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Col");
    }

    [Fact]
    public async Task UpdateCollectionAsync_ShouldUpdateCollectionInDb()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Old Name", UserId = _userId };
        _db.WordCollections.Add(collection);
        await _db.SaveChangesAsync();

        // Act
        collection.Name = "New Name";
        await _sut.UpdateCollectionAsync(collection);

        // Assert
        var updatedCollection = await _db.WordCollections.FindAsync(1);
        updatedCollection!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task DeleteCollectionAsync_ShouldRemoveCollectionFromDb()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "To Delete", UserId = _userId };
        _db.WordCollections.Add(collection);
        await _db.SaveChangesAsync();

        // Act
        await _sut.DeleteCollectionAsync(collection);

        // Assert
        var deletedCollection = await _db.WordCollections.FindAsync(1);
        deletedCollection.Should().BeNull();
    }

    [Fact]
    public async Task RenameCollectionAsync_ShouldUpdateNameAndSave()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Old Name", UserId = _userId };
        _db.WordCollections.Add(collection);
        await _db.SaveChangesAsync();

        // Act
        await _sut.RenameCollectionAsync(collection, "Renamed");

        // Assert
        var renamedCollection = await _db.WordCollections.FindAsync(1);
        renamedCollection!.Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task GetItemsForLearning_ShouldReturnAllItemsInCollection()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Col", UserId = _userId };
        var item1 = new CollectionItem
            { Id = 1, Word = "Word1", CollectionId = 1, AddedDate = DateTime.UtcNow.AddDays(-1) };
        var item2 = new CollectionItem { Id = 2, Word = "Word2", CollectionId = 1, AddedDate = DateTime.UtcNow };
        var otherItem = new CollectionItem { Id = 3, Word = "Word3", CollectionId = 2 };

        _db.WordCollections.Add(collection);
        _db.CollectionItems.AddRange(item1, item2, otherItem);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetItemsForLearning(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(i => i.Word == "Word1");
        result.Should().Contain(i => i.Word == "Word2");
    }

    [Fact]
    public async Task GetItemsDueForLearning_ShouldReturnOnlyItemsDue()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Col", UserId = _userId };
        var dueItem = new CollectionItem
        {
            Id = 1,
            CollectionId = 1,
            FlashcardProgress = new Flashcard { NextReviewDate = DateTime.UtcNow.AddDays(-1) }
        };
        var notDueItem = new CollectionItem
        {
            Id = 2,
            CollectionId = 1,
            FlashcardProgress = new Flashcard { NextReviewDate = DateTime.UtcNow.AddDays(1) }
        };

        _db.WordCollections.Add(collection);
        _db.CollectionItems.AddRange(dueItem, notDueItem);
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetItemsDueForLearning(1);

        // Assert
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(1);
    }

    [Fact]
    public async Task AddCollectionItemFromDtoAsync_ShouldAddNewItem_WhenDoesNotExist()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Col", UserId = _userId };
        _db.WordCollections.Add(collection);
        await _db.SaveChangesAsync();

        var dto = new FlashcardCreationDto
        {
            Word = "NewWord",
            PartOfSpeech = "noun",
            Definition = "A new word definition",
            Translation = "Nowe słowo"
        };

        // Act
        await _sut.AddCollectionItemFromDtoAsync(1, dto);

        // Assert
        var item = await _db.CollectionItems.FirstOrDefaultAsync(i => i.Word == "NewWord");
        item.Should().NotBeNull();
        item!.CollectionId.Should().Be(1);
        item.FlashcardProgress.Should().NotBeNull();
    }

    [Fact]
    public async Task AddCollectionItemFromDtoAsync_ShouldNotAddItem_WhenAlreadyExists()
    {
        // Arrange
        var collection = new WordCollection { Id = 1, Name = "Col", UserId = _userId };
        var existingItem = new CollectionItem
        {
            CollectionId = 1,
            Word = "ExistingWord",
            PartOfSpeech = "noun",
            Definition = "def"
        };
        _db.WordCollections.Add(collection);
        _db.CollectionItems.Add(existingItem);
        await _db.SaveChangesAsync();

        var dto = new FlashcardCreationDto
        {
            Word = "existingword", // case insensitive check
            PartOfSpeech = "noun",
            Definition = "def"
        };

        // Act
        await _sut.AddCollectionItemFromDtoAsync(1, dto);

        // Assert
        var count = await _db.CollectionItems.CountAsync(i => i.Word.ToLower() == "existingword");
        count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteCollectionItemAsync_ShouldRemoveItem()
    {
        // Arrange
        var item = new CollectionItem { Id = 1, Word = "ToDelete", CollectionId = 1 };
        _db.CollectionItems.Add(item);
        await _db.SaveChangesAsync();

        // Act
        await _sut.DeleteCollectionItemAsync(item);

        // Assert
        var deletedItem = await _db.CollectionItems.FindAsync(1);
        deletedItem.Should().BeNull();
    }

    [Fact]
    public async Task UpdateFlashcardProgress_ShouldUpdateFlashcardInDb()
    {
        // Arrange
        var flashcard = new Flashcard { Id = 1, EaseFactor = 2.5 };
        _db.Flashcards.Add(flashcard);
        await _db.SaveChangesAsync();

        // Act
        flashcard.EaseFactor = 3.0;
        await _sut.UpdateFlashcardProgress(flashcard);

        // Assert
        var updatedFlashcard = await _db.Flashcards.FindAsync(1);
        updatedFlashcard!.EaseFactor.Should().Be(3.0);
    }
}