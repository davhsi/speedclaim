using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class Claim
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid PolicyId { get; set; }
    public Guid SubmittedById { get; set; }
    public Guid? AssignedAdjusterId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ClaimedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public string? RejectionReason { get; set; }
    public string IncidentDescription { get; set; } = string.Empty;
    public short Priority { get; set; } = 3;
    public string Domain { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public bool IsAutomatedProcessed { get; set; } = false;
    public decimal RiskScore { get; set; } = 0.00m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public virtual Policy Policy { get; set; } = null!;
    public virtual User SubmittedBy { get; set; } = null!;
    public virtual User? AssignedAdjuster { get; set; }
    public virtual ICollection<ClaimWorkflow> Workflows { get; set; } = new List<ClaimWorkflow>();
    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    
    public virtual ClaimHealthDetail? HealthDetail { get; set; }
    public virtual ClaimVehicleDetail? VehicleDetail { get; set; }
    public virtual ClaimLifeDetail? LifeDetail { get; set; }
    public virtual ICollection<ClaimDocumentChecklist> DocumentChecklists { get; set; } = new List<ClaimDocumentChecklist>();
}
