using System;

namespace SpeedClaim.Api.Models;

public class ClaimVehicleDetail
{
    public Guid ClaimId { get; set; }
    public string AccidentLocation { get; set; } = string.Empty;
    public string? FirNumber { get; set; }
    public decimal RepairEstimate { get; set; }
    public bool IsTotalLoss { get; set; }
    public string? SurveyorName { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Claim Claim { get; set; } = null!;
}
