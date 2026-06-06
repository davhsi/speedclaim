using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public abstract class Policy
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; }
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? AgentId { get; set; }
    public string Status { get; set; }
    public string PaymentFrequency { get; set; } = "MONTHLY";
    public decimal PremiumAmount { get; set; }
    public decimal CoverageAmount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Domain { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; }
    public InsuranceProduct Product { get; set; }
    public Agent Agent { get; set; }
    
    public ICollection<PolicyVersion> Versions { get; set; } = new List<PolicyVersion>();
    public ICollection<PolicyInsuredMember> InsuredMembers { get; set; } = new List<PolicyInsuredMember>();
    public virtual ICollection<PremiumSchedule> PremiumSchedules { get; set; } = new List<PremiumSchedule>();
}
