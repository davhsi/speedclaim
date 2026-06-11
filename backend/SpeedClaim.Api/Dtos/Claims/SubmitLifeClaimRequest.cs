using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SpeedClaim.Api.Dtos.Claims;

public class SubmitLifeClaimRequest
{
    // Common Base Fields
    public Guid PolicyId { get; set; }
    public decimal AmountRequested { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public List<IFormFile>? Attachments { get; set; }

    // Life-Specific Fields
    public string CauseOfDeath { get; set; } = string.Empty;
    public string PlaceOfDeath { get; set; } = string.Empty;
    public string? DeathCertificateNumber { get; set; }
    public string CertifyingDoctor { get; set; } = string.Empty;
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantRelation { get; set; } = string.Empty;
}
