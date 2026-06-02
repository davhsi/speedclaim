namespace SpeedClaim.Api.Dtos.Catalog;

public record UpdateProductRequest(
    string Name,
    string Description,
    decimal? MaxCoverage,
    bool IsActive
);
