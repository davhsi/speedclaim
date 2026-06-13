using System;
using System.Collections.Generic;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class Claim
{
    public Guid Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public Guid PolicyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? ClaimantMemberId { get; set; }
    
    public ClaimType ClaimType { get; set; } = ClaimType.Health;
    public decimal ClaimAmountRequested { get; set; }
    public decimal? ClaimAmountApproved { get; set; }
    public bool IsCashless { get; set; }
    
    public ClaimStatus Status { get; set; } = ClaimStatus.Intimated;
    
    public DateTime IntimationDate { get; set; }
    public DateTime IncidentDate { get; set; }
    public string IncidentDescription { get; set; } = string.Empty;
    
    public Guid? AssignedOfficerId { get; set; }
    public Guid? SurveyorId { get; set; }
    
    public DateTime? SettlementDate { get; set; }
    public string? RejectionReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation Properties
    public virtual Policy Policy { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
    public virtual CustomerMember ClaimantMember { get; set; } = null!;
    public virtual User? AssignedOfficer { get; set; }
    public virtual Surveyor? Surveyor { get; set; }
    
    public virtual ICollection<ClaimStatusHistory> StatusHistory { get; set; } = new List<ClaimStatusHistory>();
    public virtual ICollection<SubmittedDocument> Documents { get; set; } = new List<SubmittedDocument>();
    
    public virtual HealthClaimDetail? HealthDetail { get; set; }
    public virtual MotorClaimDetail? MotorDetail { get; set; }
    public virtual LifeClaimDetail? LifeDetail { get; set; }
}
