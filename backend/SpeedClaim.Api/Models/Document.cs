using System;

namespace SpeedClaim.Api.Models;

public class Document
{
    public Guid Id { get; set; }
    public Guid? ClaimId { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid UserId { get; set; }
    
    public string Domain { get; set; } = string.Empty;
    public string DocumentTypeCode { get; set; } = string.Empty;
    
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    
    public string VerificationStatus { get; set; } = "PENDING";
    public string? RejectionReason { get; set; }
    public Guid? ReviewedById { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // Navigation Properties
    public virtual Claim? Claim { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual User? ReviewedBy { get; set; }
}
