using System;

namespace SpeedClaim.Api.Models;

public class StripeCustomer
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    public string StripeCustomerId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual User User { get; set; } = null!;
}
