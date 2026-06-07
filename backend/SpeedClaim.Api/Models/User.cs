using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string Salutation { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{Salutation} {FirstName} {LastName}".Trim();
    public string Phone { get; set; }
    public Guid AddressId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string ProfilePictureUrl { get; set; }
    public string Timezone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
    
    public string AadhaarNumber { get; set; }
    public string PanNumber { get; set; }
    public SpeedClaim.Api.Models.Enums.Gender Gender { get; set; }
    public SpeedClaim.Api.Models.Enums.MaritalStatus MaritalStatus { get; set; }
    public string KycStatus { get; set; } = "PENDING";
    
    // Navigation Properties
    public virtual Address Address { get; set; } = null!;
    public virtual Agent? Agent { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UserConsent> Consents { get; set; } = new List<UserConsent>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<PaymentStatusHistory> StatusChanges { get; set; } = new List<PaymentStatusHistory>();
}
