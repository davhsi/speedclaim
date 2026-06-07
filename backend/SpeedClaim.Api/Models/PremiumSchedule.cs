using System;

namespace SpeedClaim.Api.Models;

public class PremiumSchedule
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "PENDING"; // PENDING, PAID, OVERDUE
    public Guid? PaymentId { get; set; }

    // Navigation Properties
    public virtual Policy Policy { get; set; } = null!;
    public virtual PaymentTransaction? Payment { get; set; }
}
