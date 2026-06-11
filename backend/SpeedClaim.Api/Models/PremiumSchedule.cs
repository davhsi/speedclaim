using System;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class PremiumSchedule
{
    public Guid Id { get; set; }
    public Guid? ProposalId { get; set; }
    public Guid? PolicyId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public PremiumScheduleStatus Status { get; set; } = PremiumScheduleStatus.Upcoming;
    public Guid? PaymentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Proposal? Proposal { get; set; }
    public virtual Policy? Policy { get; set; }
    public virtual PremiumPayment? Payment { get; set; }
}
