using Microsoft.AspNetCore.Http;

namespace SpeedClaim.Api.Dtos.Common;

public class UploadDocumentRequest
{
    public IFormFile File { get; set; } = null!;
}
