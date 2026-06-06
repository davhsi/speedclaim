using System;

namespace SpeedClaim.Api.Models;

public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string StripeEventId { get; set; } = string.Empty; // For idempotency
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = string.Empty; // REQUIRES_PAYMENT, SUCCEEDED, FAILED
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Policy Policy { get; set; } = null!;
    
    public virtual ICollection<PaymentStatusHistory> StatusHistories { get; set; } = new List<PaymentStatusHistory>();
    public virtual ICollection<PremiumSchedule> PremiumSchedules { get; set; } = new List<PremiumSchedule>();
}
