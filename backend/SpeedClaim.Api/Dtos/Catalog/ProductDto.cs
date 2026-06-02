using System;

namespace SpeedClaim.Api.Dtos.Catalog;

public record ProductDto(
    Guid Id,
    string Code,
    string Name,
    string Domain,
    string Description,
    decimal? MaxCoverage,
    bool IsActive
);
