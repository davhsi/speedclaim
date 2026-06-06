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

    public User User { get; set; }
    public Role Role { get; set; }
}
