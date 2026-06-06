using System;

namespace SpeedClaim.Api.Dtos.Policies;

public record PolicyDto(
    Guid Id,
    string PolicyNumber,
    Guid UserId,
    Guid ProductId,
    Guid? AgentId,
    string Status,
    string PaymentFrequency,
    decimal PremiumAmount,
    decimal CoverageAmount,
    string Currency,
    DateTime StartDate,
    DateTime EndDate,
    string Domain,
    PolicyHealthDetailDto? HealthDetail,
    PolicyVehicleDetailDto? VehicleDetail,
    PolicyLifeDetailDto? LifeDetail
);
