using SpeedClaim.Api.Exceptions;

namespace SpeedClaim.Api.Services;

internal static class FileUploadValidator
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public static void Validate(Stream fileStream, string fileName)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ValidationException("No file provided");

        if (fileStream.Length > MaxFileSizeBytes)
            throw new ValidationException("File size exceeds the 5MB limit.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".pdf"))
            throw new ValidationException("Invalid file type. Only JPG, PNG, WebP, and PDF are allowed.");

        if (!fileStream.CanSeek)
            throw new ValidationException("The uploaded file stream must support seeking.");

        var originalPosition = fileStream.Position;
        try
        {
            fileStream.Position = 0;
            Span<byte> header = stackalloc byte[12];
            var bytesRead = fileStream.Read(header);
            if (!HasExpectedSignature(extension, header[..bytesRead]))
                throw new ValidationException("The uploaded file content does not match its file type.");
        }
        finally
        {
            fileStream.Position = originalPosition;
        }
    }

    private static bool HasExpectedSignature(string extension, ReadOnlySpan<byte> header) => extension switch
    {
        ".pdf" => header.StartsWith("%PDF-"u8),
        ".jpg" or ".jpeg" => header.Length >= 3 && header[..3].SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }),
        ".png" => header.SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ".webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
}
