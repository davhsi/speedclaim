using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using SpeedClaim.Api.Exceptions;
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
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        FileUploadValidator.Validate(fileStream, fileName);

        // Relative path starting from wwwroot
        var relativeFolderPath = Path.Combine("uploads", folderPath);
        var absoluteFolderPath = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), relativeFolderPath);
        
        if (!Directory.Exists(absoluteFolderPath))
        {
            Directory.CreateDirectory(absoluteFolderPath);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var absoluteFilePath = Path.Combine(absoluteFolderPath, uniqueFileName);

        fileStream.Position = 0;
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
            throw new NotFoundException($"File not found: {fileId}");
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
