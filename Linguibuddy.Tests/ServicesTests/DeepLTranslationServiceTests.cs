using DeepL;
using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Interfaces;
using Linguibuddy.Services;

namespace Linguibuddy.Tests.ServiceTests;

public class DeepLTranslationServiceTests
{
    private readonly IDeepLClientWrapper _client;
    private readonly DeepLTranslationService _sut;

    public DeepLTranslationServiceTests()
    {
        _client = A.Fake<IDeepLClientWrapper>();
        _sut = new DeepLTranslationService(_client);
    }

    [Fact]
    public async Task TranslateWithContextAsync_ShouldReturnTranslation_WhenClientSucceeds()
    {
        // Arrange
        var word = "dog";
        var definition = "animal";
        var partOfSpeech = "noun";
        var expectedTranslation = "pies";

        A.CallTo(() => _client.TranslateTextAsync(word, "EN", "PL", A<TextTranslateOptions>.Ignored))
            .Returns(Task.FromResult(expectedTranslation));

        // Act
        var result = await _sut.TranslateWithContextAsync(word, definition, partOfSpeech);

        // Assert
        result.Should().Be(expectedTranslation);
    }

    [Fact]
    public async Task TranslateWithContextAsync_ShouldReturnOriginalWord_WhenClientThrowsException()
    {
        // Arrange
        var word = "error";

        A.CallTo(() => _client.TranslateTextAsync(word, "EN", "PL", A<TextTranslateOptions>.Ignored))
            .Throws(new Exception("API Error"));

        // Act
        var result = await _sut.TranslateWithContextAsync(word, "def", "pos");

        // Assert
        result.Should().Be(word);
    }

    [Fact]
    public async Task TranslateWithContextAsync_ShouldReturnEmpty_WhenWordIsWhitespace()
    {
        // Act
        var result = await _sut.TranslateWithContextAsync("   ", "def", "pos");

        // Assert
        result.Should().BeEmpty();
    }
}