using System;

namespace SpeedClaim.Api.Dtos.User;

public record CreateBranchRequest(
    string Name,
    string City,
    string State,
    string Address,
    string Phone,
    string Email
);

public record BranchDto(
    Guid Id,
    string Name,
    string City,
    string State,
    string Address,
    string Phone,
    string Email,
    bool IsActive
);

public record UpdateAgentLicenseRequest(
    string LicenseNumber,
    DateOnly LicenseExpiry
);

public record AgentDashboardDto(
    int TotalCustomers,
    int TotalPolicies,
    decimal TotalCommission,
    int PendingClaims
);

public record AgentProfileDto(
    Guid AgentId,
    Guid UserId,
    string Email,
    string Salutation,
    string FirstName,
    string LastName,
    string FullName,
    string Phone,
    string AgentCode,
    string AgentType,
    string LicenseNumber,
    DateOnly LicenseExpiry,
    decimal CommissionRate,
    bool IsActive,
    string? BranchName,
    string? BranchCity,
    Guid? BranchId = null
);

public record UpdateAgentProfileRequest(
    string Salutation,
    string FirstName,
    string LastName,
    string Phone
);

public record RenewalReminderDto(
    Guid PolicyId,
    string PolicyNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    DateTime DueDate,
    decimal AmountDue,
    int DaysUntilDue
);
