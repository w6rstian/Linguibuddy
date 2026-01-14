namespace Linguibuddy.Models;

public class PexelsPhotoResponse
{
    public List<PexelsPhoto> photos { get; set; } = new();
}

public class PexelsPhoto
{
    public PexelsSource source { get; set; } = new();
}

public class PexelsSource
{
    public string medium { get; set; } = string.Empty;
}
