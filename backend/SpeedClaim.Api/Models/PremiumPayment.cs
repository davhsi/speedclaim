using System;
using System.Collections.Generic;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class PremiumPayment
{
    public Guid Id { get; set; }
    public Guid? ProposalId { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid CustomerId { get; set; }
    
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentType PaymentType { get; set; } = PaymentType.FirstPremium;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string StripeChargeId { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Card;
    
    public DateTimeOffset? PaidAt { get; set; }
    public string ReceiptUrl { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Proposal? Proposal { get; set; }
    public virtual Policy? Policy { get; set; }
    public virtual PremiumSchedule? Schedule { get; set; }
    public virtual Customer Customer { get; set; } = null!;
}
