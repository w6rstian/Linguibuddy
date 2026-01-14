using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Data;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;
using Microsoft.EntityFrameworkCore;
using System.Net;
using Linguibuddy.Tests.FakeHelpers;

namespace Linguibuddy.Tests.ServiceTests;

public class DictionaryApiServiceTests
{
    private readonly DataContext _context;
    private readonly IPexelsImageService _pexelsService;
    private readonly FakeHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private readonly DictionaryApiService _sut;

    public DictionaryApiServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new DataContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _pexelsService = A.Fake<IPexelsImageService>();
        _httpHandler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler)
        {
            BaseAddress = new Uri("https://api.dictionaryapi.dev/api/v2/entries/en/")
        };

        _sut = new DictionaryApiService(_httpClient, _context, _pexelsService);
    }

    [Fact]
    public async Task GetEnglishWordAsync_ShouldReturnFromDatabase_WhenWordExists()
    {
        // Arrange
        var word = "test";
        var expectedWord = new DictionaryWord { Word = "test", Phonetic = "test_phonetic" };
        _context.DictionaryWords.Add(expectedWord);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetEnglishWordAsync(word);

        // Assert
        result.Should().NotBeNull();
        result!.Word.Should().Be(expectedWord.Word);
        // Verify
        _httpHandler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEnglishWordAsync_ShouldCallApiAndSaveToDb_WhenWordDoesNotExistInDb()
    {
        // Arrange
        var word = "hello";
        var jsonResponse = "[{\"word\":\"hello\",\"phonetic\":\"həˈləʊ\",\"phonetics\":[{\"text\":\"həˈləʊ\",\"audio\":\"\"}],\"meanings\":[{\"partOfSpeech\":\"noun\",\"definitions\":[{\"definition\":\"greeting\"}]}]}]";
        
        _httpHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        A.CallTo(() => _pexelsService.GetImageUrlAsync(word)).Returns("https://example.com/image.jpg");

        // Act
        var result = await _sut.GetEnglishWordAsync(word);

        // Assert
        result.Should().NotBeNull();
        result!.Word.Should().Be(word);
        result.ImageUrl.Should().Be("https://example.com/image.jpg");

        // Verify
        var dbWord = await _context.DictionaryWords.FirstOrDefaultAsync(w => w.Word == word);
        dbWord.Should().NotBeNull();
        dbWord!.ImageUrl.Should().Be("https://example.com/image.jpg");
    }

    [Fact]
    public async Task GetEnglishWordAsync_ShouldReturnNull_WhenApiReturnsError()
    {
        // Arrange
        var word = "unknown";
        _httpHandler.Response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        };

        // Act
        var result = await _sut.GetEnglishWordAsync(word);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRandomWordsForGameAsync_ShouldReturnOnlyValidWords_AndLimitCount()
    {
        // Arrange
        var validWords = new List<DictionaryWord>
        {
            new() { Word = "w1", Audio = "a1", Phonetic = "p1" },
            new() { Word = "w2", Audio = "a2", Phonetic = "p2" },
            new() { Word = "w3", Audio = "a3", Phonetic = "p3" }
        };
        var invalidWord = new DictionaryWord { Word = "invalid", Audio = "", Phonetic = "" };

        _context.DictionaryWords.AddRange(validWords);
        _context.DictionaryWords.Add(invalidWord);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRandomWordsForGameAsync(2);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(w => !string.IsNullOrEmpty(w.Audio) && !string.IsNullOrEmpty(w.Phonetic));
        result.Should().NotContain(w => w.Word == "invalid");
    }

    [Fact]
    public async Task GetRandomWordsWithImagesAsync_ShouldReturnOnlyWordsWithImages()
    {
        // Arrange
        var wordWithImage = new DictionaryWord { Word = "image", Audio = "a", Phonetic = "p", ImageUrl = "url" };
        var wordWithoutImage = new DictionaryWord { Word = "noimage", Audio = "a", Phonetic = "p", ImageUrl = "" };

        _context.DictionaryWords.Add(wordWithImage);
        _context.DictionaryWords.Add(wordWithoutImage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRandomWordsWithImagesAsync(5);

        // Assert
        result.Should().HaveCount(1);
        result.First().Word.Should().Be("image");
    }
}
