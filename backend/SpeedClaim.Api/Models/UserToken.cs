using System;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class UserToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    public TokenType TokenType { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    public virtual User User { get; set; } = null!;
}
