using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

// Uploads images to Azure Blob Storage. Only the API uses this class,
// the MVC project never talks to Azure.
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

    // Uploads the file and returns its public URL
    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        string connectionString = _configuration["AzureBlobStorage:ConnectionString"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The Azure Blob Storage connection string is not configured.");
        }

        string containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        var container = new BlobContainerClient(connectionString, containerName);

        // A random name stops two uploads with the same file name from overwriting each other
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        string blobName = $"{folder}/{Guid.NewGuid()}{extension}";

        BlobClient blob = container.GetBlobClient(blobName);
        await using Stream stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        return blob.Uri.ToString();
    }
}