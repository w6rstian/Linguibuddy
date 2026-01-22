namespace Linguibuddy.Models;

public class PexelsPhotoResponse
{
    public List<PexelsPhoto> Photos { get; set; } = new();
}

public class PexelsPhoto
{
    public PexelsSource Source { get; set; } = new();
}

public class PexelsSource
{
    public string Medium { get; set; } = string.Empty;
}