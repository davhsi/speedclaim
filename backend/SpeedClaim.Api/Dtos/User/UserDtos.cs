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
    DateTime DateOfBirth,
    string Gender,
    string Relationship,
    bool IsDependent
);

public record AddFamilyMemberRequest(
    Salutation Salutation,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    Gender Gender,
    Relationship Relationship,
    bool IsDependent
);

public record UpdateFamilyMemberRequest(
    Salutation Salutation,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    Gender Gender,
    Relationship Relationship,
    bool IsDependent
);

public record KycUploadRequest(
    Guid? CustomerId,
    IdType IdType,
    string IdNumber,
    IFormFile FrontDocument,
    IFormFile? BackDocument
);

public record KycRecordDto(
    Guid Id,
    Guid UserId,
    string KycStatus,
    string IdType,
    string IdNumber,
    DateTimeOffset CreatedAt
);

public record SingleAddressRequest(
    SpeedClaim.Api.Models.Enums.AddressType AddressType,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string Pincode,
    string Country,
    bool IsSameAsPermanent
);
