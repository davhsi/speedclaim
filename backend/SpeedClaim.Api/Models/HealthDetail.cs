using System;

namespace SpeedClaim.Api.Models;

public class HealthDetail
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public Guid? PolicyId { get; set; }
    
    public string PreExistingConditions { get; set; } = string.Empty;
    public string NetworkHospitalCoverage { get; set; } = string.Empty;
    public string TpaName { get; set; } = string.Empty;
    public decimal RoomRentLimit { get; set; }
    public bool MaternityCovered { get; set; }
    public decimal CopayPercentage { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    public virtual Proposal Proposal { get; set; } = null!;
    public virtual Policy? Policy { get; set; }
}
