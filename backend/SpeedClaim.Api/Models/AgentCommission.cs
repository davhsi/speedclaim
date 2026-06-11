using System;

namespace SpeedClaim.Api.Models;

public class AgentCommission
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public Guid PolicyId { get; set; }
    public Guid PremiumPaymentId { get; set; }
    
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    
    public string Status { get; set; } = "PENDING"; // ENUM: pending, approved, paid
    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Agent Agent { get; set; } = null!;
    public Policy Policy { get; set; } = null!;
    public PremiumPayment PremiumPayment { get; set; } = null!;
}
