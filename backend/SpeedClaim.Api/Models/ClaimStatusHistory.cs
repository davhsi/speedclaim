using System;

namespace SpeedClaim.Api.Models;

public class ClaimStatusHistory
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    
    public SpeedClaim.Api.Models.Enums.ClaimStatus OldStatus { get; set; }
    public SpeedClaim.Api.Models.Enums.ClaimStatus NewStatus { get; set; }
    public Guid? ChangedById { get; set; }
    public string Notes { get; set; } = string.Empty;
    
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation Properties
    public virtual Claim Claim { get; set; } = null!;
    public virtual User? ChangedBy { get; set; }
}
