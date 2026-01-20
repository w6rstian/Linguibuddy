using System.Diagnostics;
using Linguibuddy.Interfaces;
using PexelsDotNetSDK.Api;
// Namespace z paczki NuGet

namespace Linguibuddy.Services;

public class PexelsImageService : IPexelsImageService
{
    private readonly IPexelsClientWrapper _pexelsClient;

    public PexelsImageService(IPexelsClientWrapper pexelsClient)
    {
        _pexelsClient = pexelsClient;
    }

    public async Task<string?> GetImageUrlAsync(string word)
    {
        try
        {
            var result = await _pexelsClient.SearchPhotosAsync(word, pageSize: 1);

            var photo = result?.Photos?.FirstOrDefault();

            if (photo != null) return photo.Source.Medium;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Pexels SDK Exception] {ex.Message}");
        }

        return null;
    }
}