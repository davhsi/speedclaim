using System;

namespace SpeedClaim.Api.Models;

public class Endorsement
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    
    public SpeedClaim.Api.Models.Enums.EndorsementType EndorsementType { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public string? OldValue { get; set; } // JSONB
    public string? NewValue { get; set; } // JSONB
    
    public SpeedClaim.Api.Models.Enums.EndorsementStatus Status { get; set; } = SpeedClaim.Api.Models.Enums.EndorsementStatus.Requested;
    
    public Guid RequestedById { get; set; }
    public Guid? ReviewedById { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual Policy Policy { get; set; } = null!;
    public virtual User RequestedBy { get; set; } = null!;
    public virtual User? ReviewedBy { get; set; }
}
