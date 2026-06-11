using System;

namespace SpeedClaim.Api.Models;

public class Address
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public SpeedClaim.Api.Models.Enums.AddressType AddressType { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
    public bool IsSameAsPermanent { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
