using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class Customer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    public DateOnly? DateOfBirth { get; set; }
    public SpeedClaim.Api.Models.Enums.Gender Gender { get; set; }
    public string Occupation { get; set; } = string.Empty;
    public decimal AnnualIncome { get; set; }
    public SpeedClaim.Api.Models.Enums.MaritalStatus MaritalStatus { get; set; }

    // Set when an agent onboards this customer directly (AgentsController/AuthService.AddCustomerAsync),
    // rather than the customer self-registering. Independent of any proposal — lets the agent see this
    // customer under "My customers" immediately, before any proposal exists.
    public Guid? OnboardingAgentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Agent? OnboardingAgent { get; set; }
    public virtual ICollection<CustomerMember> CustomerMembers { get; set; } = new List<CustomerMember>();
    public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
    public virtual ICollection<PremiumPayment> Payments { get; set; } = new List<PremiumPayment>();
    public virtual ICollection<Grievance> Grievances { get; set; } = new List<Grievance>();
}
