using SpeedClaim.Api.Dtos.Policies;

namespace SpeedClaim.Api.Dtos.Assistant;

public record AskSpeedyRequest(string Question);

public record SpeedyAssistantResponse(Guid RequestId, string Answer, string? Provider, string? Model);

public record AskSpeedyWorkspaceRequest(string Question, Guid? ConversationId = null);

public record SpeedyWorkspaceResponse(
    Guid RequestId,
    string Answer,
    string Intent,
    string Risk,
    IReadOnlyList<SpeedyWorkspaceAction> Actions,
    string? Provider,
    string? Model,
    Guid? ConversationId = null,
    string? EvidenceStatus = null,
    string? BrochureVersion = null,
    IReadOnlyList<PolicyAssistantCitationDto>? Citations = null);

public record SpeedyWorkspaceConversationDto(
    Guid Id, string Title, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<SpeedyWorkspaceMessageDto>? Messages = null);

public record SpeedyWorkspaceMessageDto(
    Guid Id, string Role, string Content, string? Intent, string? Risk,
    IReadOnlyList<SpeedyWorkspaceAction> Actions, DateTimeOffset CreatedAt,
    string? EvidenceStatus = null, string? BrochureVersion = null,
    IReadOnlyList<PolicyAssistantCitationDto>? Citations = null);

public record SpeedyWorkspaceAction(
    string Kind,
    string Label,
    string? Route,
    string Detail,
    bool RequiresConfirmation);

// This is deliberately a minimal, read-only projection. The AI service never receives
// database credentials, identities, payment methods, KYC data, or data for another customer.
public record SpeedyAssistantRequest(
    Guid RequestId,
    string Question,
    SpeedyAccountSnapshot Account,
    SpeedyCatalogSnapshot Catalog);

public record SpeedyWorkspaceRequest(
    Guid RequestId,
    string Question,
    SpeedyAccountSnapshot Account,
    SpeedyCatalogSnapshot Catalog);

public record SpeedyAccountSnapshot(
    string FirstName,
    bool IsAuthenticated,
    IReadOnlyList<SpeedyProposalSnapshot> Proposals,
    IReadOnlyList<SpeedyPolicySnapshot> Policies,
    IReadOnlyList<SpeedyPremiumSnapshot> UpcomingPremiums,
    IReadOnlyList<SpeedyClaimSnapshot> Claims,
    IReadOnlyList<SpeedyGrievanceSnapshot> Grievances,
    SpeedyKycSnapshot? Kyc = null);

// This contains only workflow state. Identity numbers, document paths, and review
// notes never leave the API for the AI service.
public record SpeedyKycSnapshot(
    string Status,
    bool AadhaarUploaded,
    bool PanUploaded);

// Proposal status is workflow metadata only. No underwriting notes, identity fields,
// documents, or decision rationale are ever sent to the AI service.
public record SpeedyProposalSnapshot(
    string ProposalNumber,
    string ProductName,
    string Status,
    DateTimeOffset SubmittedAt);

public record SpeedyPolicySnapshot(
    Guid PolicyId,
    string PolicyNumber,
    string ProductName,
    string Status,
    decimal CoverageAmount,
    decimal PremiumAmount,
    string PaymentFrequency,
    DateTime EndDate);

public record SpeedyPremiumSnapshot(
    string PolicyNumber,
    decimal Amount,
    DateTime DueDate,
    string Status);

public record SpeedyClaimSnapshot(
    string ClaimNumber,
    string PolicyNumber,
    string Status,
    DateTime IntimationDate);

// Customer-visible workflow state only. Descriptions, attachments, assignment,
// and internal resolution notes are deliberately excluded from the AI snapshot.
public record SpeedyGrievanceSnapshot(
    string GrievanceNumber,
    string Category,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt);

// Saleable catalog data is safe for guest questions. Account data remains empty for guests.
public record SpeedyCatalogSnapshot(IReadOnlyList<SpeedyProductSnapshot> Products);

public record SpeedyProductSnapshot(
    string ProductName,
    string Domain,
    string Description,
    int MinAge,
    int MaxAge,
    decimal MinSumAssured,
    decimal MaxSumAssured,
    int MinTenureYears,
    int MaxTenureYears,
    int WaitingPeriodDays,
    bool AllowsFamilyFloater,
    int MaxFamilyMembers,
    string? MotorVehicleType);
