using System;

namespace SpeedClaim.Api.Models;

public class PolicyStatusHistory
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    
    public SpeedClaim.Api.Models.Enums.PolicyStatus OldStatus { get; set; }
    public SpeedClaim.Api.Models.Enums.PolicyStatus NewStatus { get; set; }
    public Guid? ChangedById { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public virtual Policy Policy { get; set; } = null!;
    public virtual User? ChangedBy { get; set; }
}
