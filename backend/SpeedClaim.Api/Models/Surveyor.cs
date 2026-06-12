using System;
using System.Collections.Generic;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class Surveyor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    public SurveyorType SurveyorType { get; set; }
    public string? LicenseNumber { get; set; }
    public DateOnly? LicenseExpiry { get; set; }
    public SurveyorSpecialization Specialization { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Claim> Claims { get; set; } = new List<Claim>();
}
