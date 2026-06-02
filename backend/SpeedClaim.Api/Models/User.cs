using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string ProfilePictureUrl { get; set; }
    public string Timezone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
    
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public Agent Agent { get; set; }
}
