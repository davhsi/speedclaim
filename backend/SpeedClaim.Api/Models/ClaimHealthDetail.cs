using System;

namespace SpeedClaim.Api.Models;

public class ClaimHealthDetail
{
    public Guid ClaimId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatingDoctor { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public bool IsCashless { get; set; }
    public Guid? InsuredMemberId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public virtual Claim Claim { get; set; } = null!;
    public virtual PolicyInsuredMember? InsuredMember { get; set; }
}
