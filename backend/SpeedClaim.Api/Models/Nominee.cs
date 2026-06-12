using System;

namespace SpeedClaim.Api.Models;

public class Nominee
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public Guid? PolicyId { get; set; }
    
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public decimal SharePercentage { get; set; }
    
    public bool IsMinor { get; set; }
    public string? AppointeeName { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual Proposal Proposal { get; set; } = null!;
    public virtual Policy? Policy { get; set; }
}
