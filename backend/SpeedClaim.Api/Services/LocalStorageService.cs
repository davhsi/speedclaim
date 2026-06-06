using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;

    public LocalStorageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("No file provided");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Invalid file type. Only JPG, PNG, WebP, and PDF are allowed.");
        }

        if (fileStream.Length > 10 * 1024 * 1024) // 10 MB limit
        {
            throw new ArgumentException("File size exceeds the 10MB limit.");
        }

        // Relative path starting from wwwroot
        var relativeFolderPath = Path.Combine("uploads", folderPath);
        var absoluteFolderPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), relativeFolderPath);
        
        if (!Directory.Exists(absoluteFolderPath))
        {
            Directory.CreateDirectory(absoluteFolderPath);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var absoluteFilePath = Path.Combine(absoluteFolderPath, uniqueFileName);

        using (var stream = new FileStream(absoluteFilePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);
        }

        // Return the relative identifier
        return Path.Combine(relativeFolderPath, uniqueFileName).Replace("\\", "/");
    }

    public Task<Stream> GetFileAsync(string fileId)
    {
        var absoluteFilePath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), fileId);
        
        if (!File.Exists(absoluteFilePath))
        {
            throw new FileNotFoundException("File not found", fileId);
        }

        Stream stream = new FileStream(absoluteFilePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteFileAsync(string fileId)
    {
        if (string.IsNullOrEmpty(fileId)) return Task.CompletedTask;

        var absoluteFilePath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), fileId);
        
        if (File.Exists(absoluteFilePath))
        {
            File.Delete(absoluteFilePath);
        }

        return Task.CompletedTask;
    }
}
