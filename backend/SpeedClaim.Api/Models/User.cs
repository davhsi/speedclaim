using System;
using System.Collections.Generic;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public SpeedClaim.Api.Models.Enums.Salutation Salutation { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{Salutation} {FirstName} {LastName}".Trim();
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsEmailVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    // Navigation Properties
    public virtual Customer? Customer { get; set; }
    public virtual Agent? Agent { get; set; }
    public virtual Surveyor? Surveyor { get; set; }
    public virtual KycRecord? KycRecord { get; set; }
    
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
    public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
