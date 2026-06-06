namespace SpeedClaim.Api.Dtos.Common;

public record AddressDto(
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country
);
