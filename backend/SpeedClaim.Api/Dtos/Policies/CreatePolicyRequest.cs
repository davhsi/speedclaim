using System;

namespace SpeedClaim.Api.Dtos.Policies;

public record CreatePolicyRequest(
    Guid UserId,
    Guid ProductId,
    decimal PremiumAmount,
    decimal CoverageAmount,
    string PaymentFrequency,
    DateTime StartDate,
    DateTime EndDate,
    string Domain,
    PolicyHealthDetailDto? HealthDetail,
    PolicyVehicleDetailDto? VehicleDetail,
    PolicyLifeDetailDto? LifeDetail
);

public record PolicyHealthDetailDto(
    bool CoversDental,
    decimal Deductible,
    string NetworkType
);

public record PolicyVehicleDetailDto(
    string VehicleNumber,
    string Make,
    string Model,
    int ManufactureYear,
    decimal InsuredDeclaredValue,
    bool IsComprehensive
);

public record PolicyLifeDetailDto(
    string NomineeName,
    string NomineeRelation,
    string NomineePhone,
    bool HasAccidentalRider
);
