using System.Collections.Generic;

namespace SpeedClaim.Api.Dtos.Catalog;

public record PremiumRateDto(
    int AgeMin,
    int AgeMax,
    decimal SumAssuredMin,
    decimal SumAssuredMax,
    decimal AnnualPremium
);

public record UpdatePremiumRatesRequest(
    List<PremiumRateDto> Rates
);
