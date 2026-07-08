using System;
using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.User;

public record FamilyMemberDto(
    Guid Id,
    string Salutation,
    string FirstName,
    string LastName,
    string FullName,
    DateOnly DateOfBirth,
    string Gender,
    string Relationship,
    bool IsDependent
);

public record AddFamilyMemberRequest(
    Salutation Salutation,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    Relationship Relationship,
    bool IsDependent
);

public record UpdateFamilyMemberRequest(
    Salutation Salutation,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    Relationship Relationship,
    bool IsDependent
);

public record AadhaarUploadRequest(
    Guid? CustomerId,
    string AadhaarNumber,
    IFormFile Document
);

public record PanUploadRequest(
    Guid? CustomerId,
    string PanNumber,
    IFormFile Document
);

public record KycRecordDto(
    Guid Id,
    Guid UserId,
    string KycStatus,
    bool AadhaarUploaded,
    string? AadhaarNumber,
    bool PanUploaded,
    string? PanNumber,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    string? AadhaarDocumentPath = null,
    string? PanDocumentPath = null
);

public record KycIdentityRevealDto(
    string? AadhaarNumber,
    string? PanNumber
);

public record SingleAddressRequest(
    SpeedClaim.Api.Models.Enums.AddressType AddressType,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsSameAsPermanent
);

public record SurveyorDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName
);

public record SurveyorProfileDto(
    Guid SurveyorId,
    Guid UserId,
    string Email,
    string FullName,
    string Phone,
    string? LicenseNumber,
    DateOnly? LicenseExpiry,
    string Specialization,
    string SurveyorType,
    bool IsActive
);
