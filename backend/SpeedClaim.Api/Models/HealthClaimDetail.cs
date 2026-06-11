using System;

namespace SpeedClaim.Api.Models;

public class HealthClaimDetail
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    
    public string HospitalName { get; set; } = string.Empty;
    public string HospitalAddress { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatmentType { get; set; } = string.Empty;
    
    public string TpaReferenceNumber { get; set; } = string.Empty;
    public DateTimeOffset? PreAuthRequestedAt { get; set; }
    public DateTimeOffset? PreAuthApprovedAt { get; set; }

    // Navigation Properties
    public virtual Claim Claim { get; set; } = null!;
}
