using System;

namespace SpeedClaim.Api.Models;

public class PaymentStatusHistory
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public Guid? ChangedById { get; set; }
    public string? Remarks { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual PaymentTransaction Payment { get; set; } = null!;
    public virtual User? ChangedBy { get; set; }
}
