using FakeItEasy;
using FluentAssertions;
using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using Linguibuddy.Services;

namespace Linguibuddy.Tests.ServiceTests;

public class PexelsImageServiceTests
{
    private readonly IPexelsClientWrapper _pexelsClientWrapper;
    private readonly PexelsImageService _sut;

    public PexelsImageServiceTests()
    {
        _pexelsClientWrapper = A.Fake<IPexelsClientWrapper>();
        _sut = new PexelsImageService(_pexelsClientWrapper);
    }

    [Fact]
    public async Task GetImageUrlAsync_ShouldReturnMediumUrl_WhenPhotoIsFound()
    {
        // Arrange
        var word = "test";
        var expectedUrl = "https://example.com/medium.jpg";
        
        var mockResponse = new PexelsPhotoResponse
        {
            photos = new List<PexelsPhoto>
            {
                new PexelsPhoto
                {
                    source = new PexelsSource { medium = expectedUrl }
                }
            }
        };

        A.CallTo(() => _pexelsClientWrapper.SearchPhotosAsync(word, 1))
            .Returns(Task.FromResult<PexelsPhotoResponse?>(mockResponse));

        // Act
        var result = await _sut.GetImageUrlAsync(word);

        // Assert
        result.Should().Be(expectedUrl);
    }

    [Fact]
    public async Task GetImageUrlAsync_ShouldReturnNull_WhenNoPhotosFound()
    {
        // Arrange
        var word = "test";
        var mockResponse = new PexelsPhotoResponse
        {
            photos = new List<PexelsPhoto>()
        };

        A.CallTo(() => _pexelsClientWrapper.SearchPhotosAsync(word, 1))
            .Returns(Task.FromResult<PexelsPhotoResponse?>(mockResponse));

        // Act
        var result = await _sut.GetImageUrlAsync(word);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetImageUrlAsync_ShouldReturnNull_WhenResultIsNull()
    {
        // Arrange
        var word = "test";

        A.CallTo(() => _pexelsClientWrapper.SearchPhotosAsync(word, 1))
            .Returns(Task.FromResult<PexelsPhotoResponse?>(null));

        // Act
        var result = await _sut.GetImageUrlAsync(word);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetImageUrlAsync_ShouldReturnNull_WhenExceptionOccurs()
    {
        // Arrange
        var word = "error";

        A.CallTo(() => _pexelsClientWrapper.SearchPhotosAsync(word, 1))
            .Throws(new Exception("API Error"));

        // Act
        var result = await _sut.GetImageUrlAsync(word);

        // Assert
        result.Should().BeNull();
    }
}
