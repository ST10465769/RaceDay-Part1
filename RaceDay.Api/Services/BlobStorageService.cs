using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

// Uploads and reads images in Azure Blob Storage. Only the API uses this class,
// the MVC project never talks to Azure. The container is private.
public class BlobStorageService
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IConfiguration _configuration;

    public BlobStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Checks the file before I upload it. Returns an error message, or null if it is fine.
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "No file was supplied.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant()) || !AllowedExtensions.Contains(extension))
        {
            return "Only JPG, PNG, GIF or WebP images are allowed.";
        }

        return null;
    }

    // Only these two folders can be read back through the API
    public static bool IsAllowedBlobName(string blobName)
    {
        return !blobName.Contains("..") &&
               (blobName.StartsWith("event-banners/") || blobName.StartsWith("profile-pictures/"));
    }

    private BlobContainerClient GetContainer()
    {
        string connectionString = _configuration["AzureBlobStorage:ConnectionString"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The Azure Blob Storage connection string is not configured.");
        }

        string containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        return new BlobContainerClient(connectionString, containerName);
    }

    // Uploads the file and returns its blob URL (stored in the database)
    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        var container = GetContainer();

        // A random name stops two uploads with the same file name from overwriting each other
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string blobName = $"{folder}/{Guid.NewGuid()}{extension}";

        BlobClient blob = container.GetBlobClient(blobName);
        await using Stream stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }

    // Reads an image back so the API can send it to the browser. Returns null if it doesn't exist.
    public async Task<(Stream Content, string ContentType)?> DownloadAsync(string blobName)
    {
        BlobClient blob = GetContainer().GetBlobClient(blobName);
        if (!await blob.ExistsAsync())
        {
            return null;
        }

        var result = await blob.DownloadStreamingAsync();
        return (result.Value.Content, result.Value.Details.ContentType);
    }
}