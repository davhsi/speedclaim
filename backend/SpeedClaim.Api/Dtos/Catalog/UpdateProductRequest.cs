namespace SpeedClaim.Api.Dtos.Catalog;

public record UpdateProductRequest(
    string ProductName,
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
    string? MotorVehicleType = null,
    List<decimal>? CoverageOptions = null,
    decimal? SumAssuredIncrement = null
);
