using System;

namespace SpeedClaim.Api.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // policy, payment, claim, kyc, grievance, general
    
    public bool IsRead { get; set; }
    public string? RedirectUrl { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual User User { get; set; } = null!;
}
