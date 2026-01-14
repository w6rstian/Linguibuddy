using Linguibuddy.Interfaces;
using Linguibuddy.Models;
using PexelsDotNetSDK.Api;
using System.Diagnostics;

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
        // Here we assume the SDK call works.
        // We catch exceptions only if we want to handle them here, but the service also has try-catch.
        // However, to mimic the SDK result being mapped to our result, we need to succeed first.
        
        var result = await _client.SearchPhotosAsync(query, pageSize: pageSize);
        
        if (result == null) return null;

        var response = new PexelsPhotoResponse();
        
        if (result.photos != null)
        {
            response.photos = result.photos.Select(p => new PexelsPhoto
            {
                source = new PexelsSource { medium = p.source.medium }
            }).ToList();
        }
        
        return response;
    }
}
