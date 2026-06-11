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
    DateTime LicenseExpiry
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
    string FullName,
    string AgentCode,
    string AgentType,
    string LicenseNumber,
    DateTime LicenseExpiry,
    decimal CommissionRate,
    bool IsActive,
    string? BranchName,
    string? BranchCity
);

public record RenewalReminderDto(
    Guid PolicyId,
    string PolicyNumber,
    Guid CustomerId,
    string CustomerName,
    DateTime DueDate,
    decimal AmountDue,
    int DaysUntilDue
);
