using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public class LocalFileService : IFileService
{
    private readonly IWebHostEnvironment _env;

    public LocalFileService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> UploadProfilePictureAsync(IFormFile file, string userId)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file provided");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Invalid file type. Only JPG, PNG, and WebP are allowed.");
        }

        if (file.Length > 5 * 1024 * 1024) // 5 MB
        {
            throw new ArgumentException("File size exceeds the 5MB limit.");
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "profiles");
        
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return uniqueFileName;
    }

    public Task DeleteProfilePictureAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return Task.CompletedTask;

        var filePath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "profiles", fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public string GetProfilePictureUrl(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return string.Empty;
        
        // This generates a relative URL assuming static files are served from wwwroot
        return $"/uploads/profiles/{fileName}";
    }
}
