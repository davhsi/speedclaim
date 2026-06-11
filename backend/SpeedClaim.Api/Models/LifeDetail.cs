using System;

namespace SpeedClaim.Api.Models;

public class LifeDetail
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public Guid? PolicyId { get; set; }
    
    public string PolicySubtype { get; set; } = string.Empty;
    public decimal MaturityBenefit { get; set; }
    public decimal DeathBenefit { get; set; }
    public bool SurrenderValueApplicable { get; set; }
    public bool LoanEligible { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    public virtual Proposal Proposal { get; set; } = null!;
    public virtual Policy? Policy { get; set; }
}
