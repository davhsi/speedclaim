using System;

namespace SpeedClaim.Api.Models;

public class MotorDetail
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public Guid? PolicyId { get; set; }
    
    public string VehicleNumber { get; set; } = string.Empty;
    public string VehicleMake { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public int ManufactureYear { get; set; }
    public string VehicleType { get; set; } = string.Empty;
    public decimal Idv { get; set; }
    public string EngineNumber { get; set; } = string.Empty;
    public string ChassisNumber { get; set; } = string.Empty;
    public string CoverType { get; set; } = string.Empty;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    public virtual Proposal Proposal { get; set; } = null!;
    public virtual Policy? Policy { get; set; }
}
