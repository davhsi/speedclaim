using Microsoft.AspNetCore.Http;

namespace SpeedClaim.Api.Interfaces;

public interface IFileService
{
    Task<string> UploadProfilePictureAsync(IFormFile file, string userId);
    Task DeleteProfilePictureAsync(string fileName);
    string GetProfilePictureUrl(string fileName);
}
