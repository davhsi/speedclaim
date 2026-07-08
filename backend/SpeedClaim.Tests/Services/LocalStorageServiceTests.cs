using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Moq;
using NUnit.Framework;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class LocalStorageServiceTests
{
    private Mock<IWebHostEnvironment> _envMock;
    private LocalStorageService _storageService;
    private string _tempDirectory;

    [SetUp]
    public void SetUp()
    {
        _envMock = new Mock<IWebHostEnvironment>();
        
        // Create a unique temporary directory for this test run
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        _envMock.Setup(e => e.WebRootPath).Returns(_tempDirectory);

        _storageService = new LocalStorageService(_envMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up the temporary directory after test
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public async Task UploadFileAsync_ValidFile_SavesFileAndReturnsRelativePath()
    {
        // Arrange
        var content = "This is a test document";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var fileName = "test.pdf";
        var folderPath = "kyc/123";

        // Act
        var relativePath = await _storageService.UploadFileAsync(stream, fileName, folderPath);

        // Assert
        Assert.That(relativePath, Is.Not.Null);
        Assert.That(relativePath, Does.StartWith("uploads/kyc/123/"));
        Assert.That(relativePath, Does.EndWith(".pdf"));

        // Verify physical file exists
        var absolutePath = Path.Combine(_tempDirectory, relativePath);
        Assert.That(File.Exists(absolutePath), Is.True);
        
        var savedContent = await File.ReadAllTextAsync(absolutePath);
        Assert.That(savedContent, Is.EqualTo(content));
    }

    [Test]
    public void UploadFileAsync_NullStream_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(async () => 
            await _storageService.UploadFileAsync(null!, "test.pdf", "folder"));
        
        Assert.That(ex.Message, Does.Contain("No file provided"));
    }

    [Test]
    public void UploadFileAsync_EmptyStream_ThrowsArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act & Assert
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(async () => 
            await _storageService.UploadFileAsync(stream, "test.pdf", "folder"));
        
        Assert.That(ex.Message, Does.Contain("No file provided"));
    }

    [Test]
    public void UploadFileAsync_InvalidExtension_ThrowsArgumentException()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        
        // Act & Assert
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(async () => 
            await _storageService.UploadFileAsync(stream, "test.exe", "folder"));
        
        Assert.That(ex.Message, Does.Contain("Invalid file type"));
    }

    [Test]
    public void UploadFileAsync_ExceedsSizeLimit_ThrowsArgumentException()
    {
        // Arrange
        // Create a mock stream that reports length > 5MB
        var mockStream = new Mock<Stream>();
        mockStream.Setup(s => s.Length).Returns(6 * 1024 * 1024); // 6 MB

        // Act & Assert
        var ex = Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.ValidationException>(async () =>
            await _storageService.UploadFileAsync(mockStream.Object, "test.pdf", "folder"));

        Assert.That(ex.Message, Does.Contain("exceeds the 5MB limit"));
    }

    [Test]
    public async Task GetFileAsync_ExistingFile_ReturnsStream()
    {
        // Arrange
        var content = "existing content";
        var relativePath = "uploads/test/doc.pdf";
        var absolutePath = Path.Combine(_tempDirectory, relativePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await File.WriteAllTextAsync(absolutePath, content);

        // Act
        using var stream = await _storageService.GetFileAsync(relativePath);
        using var reader = new StreamReader(stream);
        var readContent = await reader.ReadToEndAsync();

        // Assert
        Assert.That(readContent, Is.EqualTo(content));
    }

    [Test]
    public void GetFileAsync_NonExistingFile_ThrowsFileNotFoundException()
    {
        // Act & Assert
        Assert.ThrowsAsync<SpeedClaim.Api.Exceptions.NotFoundException>(async () =>
            await _storageService.GetFileAsync("nonexistent.pdf"));
    }

    [Test]
    public async Task DeleteFileAsync_ExistingFile_DeletesFile()
    {
        // Arrange
        var relativePath = "uploads/test/delete.pdf";
        var absolutePath = Path.Combine(_tempDirectory, relativePath);
        
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await File.WriteAllTextAsync(absolutePath, "content");
        
        Assert.That(File.Exists(absolutePath), Is.True);

        // Act
        await _storageService.DeleteFileAsync(relativePath);

        // Assert
        Assert.That(File.Exists(absolutePath), Is.False);
    }

    [Test]
    public async Task DeleteFileAsync_NullOrEmptyId_DoesNothing()
    {
        // Act & Assert - Should not throw
        await _storageService.DeleteFileAsync(null!);
        await _storageService.DeleteFileAsync("");
    }

    [Test]
    public async Task DeleteFileAsync_NonExistingFile_DoesNothing()
    {
        // Act & Assert - Should not throw
        await _storageService.DeleteFileAsync("does_not_exist.pdf");
    }
}
