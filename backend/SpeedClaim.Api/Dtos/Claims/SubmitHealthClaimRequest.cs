using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace SpeedClaim.Api.Dtos.Claims;

public class SubmitHealthClaimRequest
{
    // Common Base Fields
    public Guid PolicyId { get; set; }
    public decimal AmountRequested { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public List<IFormFile>? Attachments { get; set; }

    // Health-Specific Fields
    public string HospitalName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatingDoctor { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public bool IsCashless { get; set; }
    public Guid? InsuredMemberId { get; set; }
}
