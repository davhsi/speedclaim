using System;

namespace SpeedClaim.Api.Models;

public class ClaimLifeDetail
{
    public Guid ClaimId { get; set; }
    public string CauseOfDeath { get; set; } = string.Empty;
    public string PlaceOfDeath { get; set; } = string.Empty;
    public string? DeathCertificateNumber { get; set; }
    public string CertifyingDoctor { get; set; } = string.Empty;
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantRelation { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual Claim Claim { get; set; } = null!;
}
