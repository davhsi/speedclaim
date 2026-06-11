using System;

namespace SpeedClaim.Api.Models;

public class ProposalMember
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public Guid CustomerMemberId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual Proposal Proposal { get; set; } = null!;
    public virtual CustomerMember CustomerMember { get; set; } = null!;
}
