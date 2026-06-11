using System;

namespace SpeedClaim.Api.Models;

public class MotorClaimDetail
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    
    public string AccidentLocation { get; set; } = string.Empty;
    public string? FirNumber { get; set; }
    public string GarageName { get; set; } = string.Empty;
    public decimal EstimatedRepairCost { get; set; }
    public DateTime? SurveyDate { get; set; }
    public string? SurveyorRemarks { get; set; }

    // Navigation Properties
    public virtual Claim Claim { get; set; } = null!;
}
