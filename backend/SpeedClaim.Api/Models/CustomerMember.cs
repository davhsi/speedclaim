using System;

namespace SpeedClaim.Api.Models;

public class CustomerMember
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    
    public SpeedClaim.Api.Models.Enums.Salutation Salutation { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{Salutation} {FirstName} {LastName}".Trim();
    
    public DateOnly DateOfBirth { get; set; }
    public SpeedClaim.Api.Models.Enums.Gender Gender { get; set; }
    public SpeedClaim.Api.Models.Enums.Relationship Relationship { get; set; }
    public bool IsDependent { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
