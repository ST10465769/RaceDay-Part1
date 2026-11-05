namespace RaceDay.Mvc.Helpers;

public static class ImageUrlExtensions
{
    // The database stores the Azure blob URL, but the container is private, so the browser
    // can't open it. This turns it into my own /Images/... route, which asks the API for the image.
    public static string? ImageSrc(this string? blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl) || !Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        // The path looks like /raceday-images/event-banners/abc.jpg. I drop the container name.
        var parts = uri.AbsolutePath.Trim('/').Split('/', 2);
        return parts.Length == 2 ? "/Images/" + parts[1] : null;
    }
}
