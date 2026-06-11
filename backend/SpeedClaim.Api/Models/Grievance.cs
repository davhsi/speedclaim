using System;

namespace SpeedClaim.Api.Models;

public class Grievance
{
    public Guid Id { get; set; }
    public string GrievanceNumber { get; set; } = string.Empty;
    
    public Guid CustomerId { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid? ClaimId { get; set; }
    
    public SpeedClaim.Api.Models.Enums.GrievanceCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public SpeedClaim.Api.Models.Enums.GrievanceStatus Status { get; set; }
    
    public Guid? AssignedToId { get; set; }
    
    public string? ResolutionNotes { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual Customer Customer { get; set; } = null!;
    public virtual Policy? Policy { get; set; }
    public virtual Claim? Claim { get; set; }
    public virtual User? AssignedTo { get; set; }
}
