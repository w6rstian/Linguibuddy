using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using PexelsDotNetSDK.Api;

namespace Linguibuddy.Services;

public class PexelsClientWrapper : IPexelsClientWrapper
{
    private readonly PexelsClient _client;

    public PexelsClientWrapper(PexelsClient client)
    {
        _client = client;
    }

    public async Task<PexelsPhotoResponse?> SearchPhotosAsync(string query, int pageSize = 1)
    {
        var result = await _client.SearchPhotosAsync(query, pageSize: pageSize);

        if (result == null) return null;

        var response = new PexelsPhotoResponse();

        if (result.photos != null)
            response.Photos = result.photos.Select(p => new PexelsPhoto
            {
                Source = new PexelsSource { Medium = p.source.medium }
            }).ToList();

        return response;
    }
}