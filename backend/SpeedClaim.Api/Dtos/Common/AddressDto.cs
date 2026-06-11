namespace SpeedClaim.Api.Dtos.Common;

public record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country
);
