using System;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class SubmittedDocument
{
    public Guid Id { get; set; }
    public EntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string DocumentKey { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public string StoredFilename { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int FileSizeKb { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
}
