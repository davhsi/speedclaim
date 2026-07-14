using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public class AzureBlobStorageService : IStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".pdf"
    };

    private readonly BlobContainerClient _container;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureBlob:ConnectionString"];
        var containerName = configuration["AzureBlob:ContainerName"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AzureBlob:ConnectionString must be configured when Storage:Provider is AzureBlob.");

        if (string.IsNullOrWhiteSpace(containerName))
            throw new InvalidOperationException("AzureBlob:ContainerName must be configured when Storage:Provider is AzureBlob.");

        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
    {
        ValidateFile(fileStream, fileName);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var blobName = BuildBlobName(folderPath, extension);
        var blob = _container.GetBlobClient(blobName);

        fileStream.Position = 0;
        await blob.UploadAsync(fileStream, new BlobHttpHeaders
        {
            ContentType = GetContentType(extension)
        });

        return blobName;
    }

    public async Task<Stream> GetFileAsync(string fileId)
    {
        var blobName = NormalizeBlobName(fileId);
        var blob = _container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync())
            throw new NotFoundException($"File not found: {fileId}");

        var download = await blob.DownloadStreamingAsync();
        return download.Value.Content;
    }

    public async Task DeleteFileAsync(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId)) return;

        var blobName = NormalizeBlobName(fileId);
        var blob = _container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync();
    }

    private static void ValidateFile(Stream fileStream, string fileName)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ValidationException("No file provided");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ValidationException("Invalid file type. Only JPG, PNG, WebP, and PDF are allowed.");

        if (fileStream.Length > 5 * 1024 * 1024)
            throw new ValidationException("File size exceeds the 5MB limit.");
    }

    private static string BuildBlobName(string folderPath, string extension)
    {
        var normalizedFolder = folderPath
            .Replace('\\', '/')
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        return $"uploads/{string.Join('/', normalizedFolder)}/{Guid.NewGuid()}{extension}";
    }

    private static string NormalizeBlobName(string fileId)
    {
        return fileId
            .Replace('\\', '/')
            .TrimStart('/');
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
