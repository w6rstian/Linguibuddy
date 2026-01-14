using Linguibuddy.Models;

namespace Linguibuddy.Interfaces;

public interface IPexelsClientWrapper
{
    Task<PexelsPhotoResponse?> SearchPhotosAsync(string query, int pageSize = 1);
}
