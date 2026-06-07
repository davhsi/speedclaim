using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class PolicyVersion
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public int VersionNumber { get; set; }
    public decimal PremiumAmount { get; set; }
    public decimal CoverageAmount { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation Properties
    public virtual Policy Policy { get; set; }
}
