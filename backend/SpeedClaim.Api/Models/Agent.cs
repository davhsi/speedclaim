using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class Agent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? BranchId { get; set; }
    
    public string AgentCode { get; set; } = string.Empty;
    public SpeedClaim.Api.Models.Enums.AgentType AgentType { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateOnly LicenseExpiry { get; set; }
    public decimal CommissionRate { get; set; }
    
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Branch? Branch { get; set; }
}
