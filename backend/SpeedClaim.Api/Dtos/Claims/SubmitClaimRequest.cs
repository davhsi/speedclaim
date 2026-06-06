using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SpeedClaim.Api.Dtos.Claims;

public class SubmitClaimRequest
{
    public Guid PolicyId { get; set; }
    public decimal AmountRequested { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public List<IFormFile>? Attachments { get; set; }
    
    // Domain-Specific Details (Optional based on domain)
    public ClaimHealthDetailDto? HealthDetail { get; set; }
    public ClaimVehicleDetailDto? VehicleDetail { get; set; }
    public ClaimLifeDetailDto? LifeDetail { get; set; }
}
