using System;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.Auth;

public record RegisterUserRequest(
    string Email,
    string Password,
    string Salutation,
    string FirstName,
    string LastName,
    string Phone,
    AddressDto Address,
    DateTime DateOfBirth,
    string AadhaarNumber,
    string PanNumber,
    Gender Gender,
    MaritalStatus MaritalStatus
)
{
    public override string ToString() => 
        $"RegisterUserRequest {{ Email = {Email}, Name = {Salutation} {FirstName} {LastName}, Phone = {Phone}, Aadhaar = {(AadhaarNumber?.Length >= 4 ? new string('X', 8) + AadhaarNumber[^4..] : "MASKED")}, Pan = MASKED, Gender = {Gender}, MaritalStatus = {MaritalStatus} }}";
}
