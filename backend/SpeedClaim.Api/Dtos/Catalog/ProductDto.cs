using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Dtos.Catalog;

public record ProductDto(
    Guid Id,
    string ProductName,
    string Domain,
    string Uin,
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
    bool IsActive,
    bool IsAvailableForSale,
    string? MotorVehicleType = null,
    IReadOnlyList<decimal>? CoverageOptions = null,
    decimal? SumAssuredIncrement = null
);
