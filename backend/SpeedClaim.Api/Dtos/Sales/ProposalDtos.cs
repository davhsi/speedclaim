using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Dtos.Sales;

public record GenerateQuoteRequest(
    string ProductId,
    int Age,
    string? Gender,
    decimal SumAssured,
    int TenureYears
);

public record GenerateQuoteResponse(
    decimal PremiumAmount,
    decimal SumAssured,
    int TenureYears,
    string PaymentFrequency
);

public record SubmitProposalRequest(
    string CustomerId,
    string ProductId,
    decimal SumAssured,
    int TenureYears,
    decimal PremiumAmount,
    string PaymentFrequency,
    HealthDetailDto? HealthDetail,
    LifeDetailDto? LifeDetail,
    MotorDetailDto? MotorDetail,
    List<string> CustomerMemberIds,
    List<NomineeDto> Nominees
);

public record NomineeDto(
    string FullName,
    string Relationship,
    DateTime DateOfBirth,
    decimal SharePercentage,
    bool IsMinor,
    string? AppointeeName
);

public record HealthDetailDto(
    string PreExistingConditions,
    string NetworkHospitalCoverage,
    string TpaName,
    decimal RoomRentLimit,
    bool MaternityCovered,
    decimal CopayPercentage
);

public record LifeDetailDto(
    string PolicySubtype,
    decimal MaturityBenefit,
    decimal DeathBenefit,
    bool SurrenderValueApplicable,
    bool LoanEligible
);

public record MotorDetailDto(
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

public record ProposalDto(
    Guid Id,
    string ProposalNumber,
    Guid CustomerId,
    Guid? AgentId,
    Guid ProductId,
    string Status,
    decimal SumAssured,
    int TenureYears,
    decimal PremiumAmount,
    string PaymentFrequency,
    DateTimeOffset CreatedAt
);

public record ApproveRejectProposalRequest(
    bool IsApproved,
    string Notes
);

public record AdditionalDocumentRequest(
    string Details
);


public record AddUnderwriterNotesRequest(
    string Notes
);
