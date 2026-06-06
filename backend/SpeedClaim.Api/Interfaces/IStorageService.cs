using System.IO;
using System.Threading.Tasks;

namespace SpeedClaim.Api.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderPath);
    Task<Stream> GetFileAsync(string fileId);
    Task DeleteFileAsync(string fileId);
}
