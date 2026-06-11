using System;

namespace SpeedClaim.Api.Models;

public class LifeClaimDetail
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    
    public string ClaimReason { get; set; } = string.Empty;
    public string CauseOfDeath { get; set; } = string.Empty;
    public string PlaceOfDeath { get; set; } = string.Empty;
    public string? DeathCertificateNumber { get; set; }
    public string CertifyingDoctor { get; set; } = string.Empty;
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantRelationship { get; set; } = string.Empty;

    // Navigation Properties
    public virtual Claim Claim { get; set; } = null!;
}
