using System;

namespace SpeedClaim.Core.Entities.Auth;

public class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public string AssignedBy { get; set; } = null!;

    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
