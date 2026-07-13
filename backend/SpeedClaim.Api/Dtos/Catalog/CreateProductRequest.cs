namespace SpeedClaim.Api.Dtos.Catalog;

public record CreateProductRequest(
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
    string? MotorVehicleType = null
);
