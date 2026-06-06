namespace SpeedClaim.Api.Dtos.Catalog;

public record CreateProductRequest(
    string Code,
    string Name,
    string Domain,
    string Description,
    decimal? MaxCoverage
);
