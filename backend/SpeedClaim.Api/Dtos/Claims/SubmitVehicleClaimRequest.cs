using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SpeedClaim.Api.Dtos.Claims;

public class SubmitVehicleClaimRequest
{
    // Common Base Fields
    public Guid PolicyId { get; set; }
    public decimal AmountRequested { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public List<IFormFile>? Attachments { get; set; }

    // Vehicle-Specific Fields
    public string AccidentLocation { get; set; } = string.Empty;
    public string? FirNumber { get; set; }
    public decimal RepairEstimate { get; set; }
    public bool IsTotalLoss { get; set; }
    public string? SurveyorName { get; set; }
}
