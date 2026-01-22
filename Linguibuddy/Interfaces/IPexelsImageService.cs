namespace Linguibuddy.Interfaces;

public interface IPexelsImageService
{
    Task<string?> GetImageUrlAsync(string word);
}