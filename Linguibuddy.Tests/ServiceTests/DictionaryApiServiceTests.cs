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
}
