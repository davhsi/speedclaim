using System;

namespace SpeedClaim.Api.Models;

public class ClaimDocumentChecklist
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string DocumentTypeCode { get; set; } = string.Empty;
    public bool IsReceived { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Claim Claim { get; set; } = null!;
}
