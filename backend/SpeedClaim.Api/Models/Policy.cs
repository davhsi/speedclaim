using System;
using System.Collections.Generic;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class Policy
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public Guid? ProposalId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    // Nullable for legacy policies. New issuance sets this once when an indexed brochure is published.
    public Guid? ProductBrochureId { get; set; }
    public Guid? AgentId { get; set; }
    
    public SpeedClaim.Api.Models.Enums.PolicyType PolicyType { get; set; }
    public decimal SumAssured { get; set; }
    public decimal PremiumAmount { get; set; }
    public string PaymentFrequency { get; set; } = "MONTHLY";
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PolicyStatus Status { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Proposal? Proposal { get; set; }
    public virtual InsuranceProduct Product { get; set; } = null!;
    public virtual ProductBrochure? ProductBrochure { get; set; }
    public virtual Agent? Agent { get; set; }
    
    public virtual HealthDetail? HealthDetail { get; set; }
    public virtual LifeDetail? LifeDetail { get; set; }
    public virtual MotorDetail? MotorDetail { get; set; }
    
    public virtual ICollection<PolicyMember> PolicyMembers { get; set; } = new List<PolicyMember>();
    public virtual ICollection<Nominee> Nominees { get; set; } = new List<Nominee>();
    public virtual ICollection<PolicyStatusHistory> StatusHistory { get; set; } = new List<PolicyStatusHistory>();
    public virtual ICollection<Endorsement> Endorsements { get; set; } = new List<Endorsement>();
    public virtual ICollection<PremiumSchedule> PremiumSchedules { get; set; } = new List<PremiumSchedule>();
    public virtual ICollection<PremiumPayment> PremiumPayments { get; set; } = new List<PremiumPayment>();
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    public virtual ICollection<PolicyAssistantConversation> AssistantConversations { get; set; } = new List<PolicyAssistantConversation>();
}
