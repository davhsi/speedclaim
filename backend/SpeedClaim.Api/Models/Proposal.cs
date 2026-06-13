using System;
using System.Collections.Generic;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class Proposal
{
    public Guid Id { get; set; }
    public string ProposalNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid? AgentId { get; set; }
    public Guid ProductId { get; set; }
    
    public SpeedClaim.Api.Models.Enums.PolicyType PolicyType { get; set; }
    public decimal SumAssured { get; set; }
    public int TenureYears { get; set; }
    public decimal PremiumAmount { get; set; }
    public string PaymentFrequency { get; set; } = string.Empty; // e.g. monthly, quarterly, annual
    
    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;
    
    public Guid? UnderwriterId { get; set; }
    public string? UnderwriterNotes { get; set; }
    public string? RejectionReason { get; set; }
    
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
    public virtual Agent? Agent { get; set; }
    public virtual InsuranceProduct Product { get; set; } = null!;
    public virtual User? Underwriter { get; set; }
    
    public virtual HealthDetail? HealthDetail { get; set; }
    public virtual LifeDetail? LifeDetail { get; set; }
    public virtual MotorDetail? MotorDetail { get; set; }
    
    public virtual ICollection<ProposalMember> ProposalMembers { get; set; } = new List<ProposalMember>();
    public virtual ICollection<Nominee> Nominees { get; set; } = new List<Nominee>();
    public virtual ICollection<PremiumSchedule> PremiumSchedules { get; set; } = new List<PremiumSchedule>();
    public virtual ICollection<PremiumPayment> PremiumPayments { get; set; } = new List<PremiumPayment>();
}
