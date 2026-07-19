namespace SpeedClaim.Api.Dtos.Assistant;

public record AskSpeedyRequest(string Question);

public record SpeedyAssistantResponse(Guid RequestId, string Answer, string? Provider, string? Model);

// This is deliberately a minimal, read-only projection. The AI service never receives
// database credentials, identities, payment methods, KYC data, or data for another customer.
public record SpeedyAssistantRequest(
    Guid RequestId,
    string Question,
    SpeedyAccountSnapshot Account,
    SpeedyCatalogSnapshot Catalog);

public record SpeedyAccountSnapshot(
    string FirstName,
    bool IsAuthenticated,
    IReadOnlyList<SpeedyPolicySnapshot> Policies,
    IReadOnlyList<SpeedyPremiumSnapshot> UpcomingPremiums,
    IReadOnlyList<SpeedyClaimSnapshot> Claims);

public record SpeedyPolicySnapshot(
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
