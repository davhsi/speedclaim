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
    IFormFile FrontDocument,
    IFormFile? BackDocument
);

public record PanUploadRequest(
    Guid? CustomerId,
    string PanNumber,
    IFormFile FrontDocument,
    IFormFile? BackDocument
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
    DateTimeOffset CreatedAt
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
