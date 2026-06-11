using Microsoft.AspNetCore.Http;

namespace SpeedClaim.Api.Dtos.Common;

public class UploadDocumentRequest
{
    public string DocumentType { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
}
