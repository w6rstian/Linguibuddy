using System.Diagnostics;
using Linguibuddy.Interfaces;
using PexelsDotNetSDK.Api;
// Namespace z paczki NuGet

namespace Linguibuddy.Services;

public class PexelsImageService : IPexelsImageService
{
    private readonly PexelsClient _pexelsClient;

    public PexelsImageService(PexelsClient pexelsClient)
    {
        _pexelsClient = pexelsClient;
    }

    public async Task<string?> GetImageUrlAsync(string word)
    {
        try
        {
            var result = await _pexelsClient.SearchPhotosAsync(word, pageSize: 1);

            var photo = result?.photos?.FirstOrDefault();

            if (photo != null) return photo.source.medium;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Pexels SDK Exception] {ex.Message}");
        }

        return null;
    }
}