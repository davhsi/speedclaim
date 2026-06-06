using System;

namespace SpeedClaim.Api.Dtos.Claims;

public class ClaimHealthDetailDto
{
    public string HospitalName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string TreatingDoctor { get; set; } = string.Empty;
    public DateTime AdmissionDate { get; set; }
    public DateTime? DischargeDate { get; set; }
    public bool IsCashless { get; set; }
    public Guid? InsuredMemberId { get; set; }
}

public class ClaimVehicleDetailDto
{
    public string AccidentLocation { get; set; } = string.Empty;
    public string? FirNumber { get; set; }
    public decimal RepairEstimate { get; set; }
    public bool IsTotalLoss { get; set; }
    public string? SurveyorName { get; set; }
}

public class ClaimLifeDetailDto
{
    public string CauseOfDeath { get; set; } = string.Empty;
    public string PlaceOfDeath { get; set; } = string.Empty;
    public string? DeathCertificateNumber { get; set; }
    public string CertifyingDoctor { get; set; } = string.Empty;
    public string ClaimantName { get; set; } = string.Empty;
    public string ClaimantRelation { get; set; } = string.Empty;
}

public class ClaimDocumentChecklistDto
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public string DocumentTypeCode { get; set; } = string.Empty;
    public bool IsReceived { get; set; }
}
