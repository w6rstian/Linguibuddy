using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Helpers;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;
using OpenAI.Chat;

namespace Linguibuddy.Tests.ServicesTests;

public class OpenAiServiceTests
{
    private readonly IOpenAiClientWrapper _clientWrapper;
    private readonly OpenAiService _service;

    public OpenAiServiceTests()
    {
        _clientWrapper = A.Fake<IOpenAiClientWrapper>();
        _service = new OpenAiService(_clientWrapper);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnResponse_WhenClientReturnsText()
    {
        // Arrange
        var expectedResponse = "TAK";
        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<string>.Ignored))
            .Returns(expectedResponse);

        // Act
        var result = await _service.TestConnectionAsync();

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnError_WhenClientThrowsException()
    {
        // Arrange
        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<string>.Ignored))
            .Throws(new Exception("Connection failed"));

        // Act
        var result = await _service.TestConnectionAsync();

        // Assert
        result.Should().Contain("Error: Connection failed");
    }

    [Fact]
    public async Task TranslateWithContextAsync_ShouldReturnTranslation_WhenClientReturnsText()
    {
        // Arrange
        var word = "dog";
        var definition = "an animal";
        var partOfSpeech = "noun";
        var expectedTranslation = "pies";

        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<IEnumerable<ChatMessage>>.Ignored))
            .Returns(expectedTranslation);

        // Act
        var result = await _service.TranslateWithContextAsync(word, definition, partOfSpeech);

        // Assert
        result.Should().Be(expectedTranslation);
    }

    [Fact]
    public async Task TranslateWithContextAsync_ShouldReturnOriginalWord_WhenClientThrowsException()
    {
        // Arrange
        var word = "dog";
        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<IEnumerable<ChatMessage>>.Ignored))
            .Throws(new Exception("API Error"));

        // Act
        var result = await _service.TranslateWithContextAsync(word, "def", "noun");

        // Assert
        result.Should().Be(word);
    }

    [Fact]
    public async Task GenerateSentenceAsync_ShouldReturnSentence_WhenClientReturnsValidJson()
    {
        // Arrange
        var targetWord = "apple";
        var difficulty = "A1";
        var jsonResponse = "{\"english_sentence\": \"I eat an apple.\", \"polish_translation\": \"Jem jabłko.\"}";

        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<IEnumerable<ChatMessage>>.Ignored))
            .Returns(jsonResponse);

        // Act
        var result = await _service.GenerateSentenceAsync(targetWord, difficulty);

        // Assert
        result.Should().NotBeNull();
        result!.Value.English.Should().Be("I eat an apple.");
        result!.Value.Polish.Should().Be("Jem jabłko.");
    }

    [Fact]
    public async Task GenerateSentenceAsync_ShouldReturnNull_WhenClientReturnsInvalidJson()
    {
        // Arrange
        var targetWord = "apple";
        var jsonResponse = "Invalid JSON";

        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<IEnumerable<ChatMessage>>.Ignored))
            .Returns(jsonResponse);

        // Act
        var result = await _service.GenerateSentenceAsync(targetWord, "A1");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeCollectionProgressAsync_ShouldReturnAnalysis_WhenCollectionIsNotEmpty()
    {
        // Arrange
        var collection = new WordCollection
        {
            Name = "Test Collection",
            Items = new List<CollectionItem> { new() }
        };
        var expectedAnalysis = "Analysis result";

        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<IEnumerable<ChatMessage>>.Ignored))
            .Returns(expectedAnalysis);

        // Act
        var result = await _service.AnalyzeCollectionProgressAsync(collection, DifficultyLevel.A1, "en");

        // Assert
        result.Should().Be(expectedAnalysis);
    }

    [Fact]
    public async Task AnalyzeCollectionProgressAsync_ShouldReturnDefaultMessage_WhenCollectionIsEmpty()
    {
        // Arrange
        var collection = new WordCollection { Items = new List<CollectionItem>() };

        // Act
        var result = await _service.AnalyzeCollectionProgressAsync(collection, DifficultyLevel.A1, "en");

        // Assert
        result.Should().Contain("Ta kolekcja jest pusta");
        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<IEnumerable<ChatMessage>>.Ignored))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task AnalyzeComprehensiveProfileAsync_ShouldReturnReport_WhenUserExists()
    {
        // Arrange
        var user = new AppUser { Id = "test-user", Points = 100 };
        var collections = new List<WordCollection>();
        var expectedReport = "Comprehensive Report";

        A.CallTo(() => _clientWrapper.CompleteChatAsync(A<IEnumerable<ChatMessage>>.Ignored))
            .Returns(expectedReport);

        // Act
        var result = await _service.AnalyzeComprehensiveProfileAsync(user, 5, 2, collections, "en");

        // Assert
        result.Should().Be(expectedReport);
    }
}