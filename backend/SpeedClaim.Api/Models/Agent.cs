using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class Agent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string AgencyName { get; set; } = string.Empty;
    public DateTime LicenseValidUntil { get; set; }
    public decimal CommissionRate { get; set; } = 0.0000m;
    public bool IsActive { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
