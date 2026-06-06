using System;

namespace SpeedClaim.Api.Models;

public class ClaimWorkflow
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public Guid ActorId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime TransitionedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Claim Claim { get; set; } = null!;
    public virtual User Actor { get; set; } = null!;
}
