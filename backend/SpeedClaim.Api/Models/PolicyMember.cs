using System;

namespace SpeedClaim.Api.Models;

public class PolicyMember
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public Guid CustomerMemberId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual Policy Policy { get; set; } = null!;
    public virtual CustomerMember CustomerMember { get; set; } = null!;
}
