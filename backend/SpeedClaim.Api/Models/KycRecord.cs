using System;

namespace SpeedClaim.Api.Models;

public class KycRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    public SpeedClaim.Api.Models.Enums.KycStatus KycStatus { get; set; }
    public SpeedClaim.Api.Models.Enums.IdType IdType { get; set; }
    public string IdNumber { get; set; } = string.Empty;
    
    public Guid? ReviewedById { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual User? ReviewedBy { get; set; }
}
