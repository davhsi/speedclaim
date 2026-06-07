using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public string Domain { get; set; }
    public decimal? ApprovalLimit { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    // Navigation Properties
    public virtual User User { get; set; }
    public virtual Role Role { get; set; }
}
