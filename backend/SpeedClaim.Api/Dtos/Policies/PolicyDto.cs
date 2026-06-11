using System;
using System.Text.Json.Serialization;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.Policies;

public record PolicyDto(
    Guid Id,
    string PolicyNumber,
    Guid UserId,
    Guid ProductId,
    Guid? AgentId,
    PolicyStatus Status,
    string PaymentFrequency,
    decimal PremiumAmount,
    decimal CoverageAmount,
    string Currency,
    DateTime StartDate,
    DateTime EndDate,
    string Domain,
    
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PolicyHealthDetailDto? HealthDetail,
    
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PolicyVehicleDetailDto? VehicleDetail,
    
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PolicyLifeDetailDto? LifeDetail
);

public record PolicyHealthDetailDto(
    string PreExistingConditions,
    string NetworkHospitalCoverage,
    string TpaName,
    decimal RoomRentLimit,
    bool MaternityCovered,
    decimal CopayPercentage
);

public record PolicyVehicleDetailDto(
    string VehicleNumber,
    string VehicleMake,
    string VehicleModel,
    int ManufactureYear,
    string VehicleType,
    decimal Idv,
    string EngineNumber,
    string ChassisNumber,
    string CoverType
);

public record PolicyLifeDetailDto(
    string PolicySubtype,
    decimal MaturityBenefit,
    decimal DeathBenefit,
    bool SurrenderValueApplicable,
    bool LoanEligible
);
