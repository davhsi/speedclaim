using System;

namespace SpeedClaim.Api.Dtos.Financial;

public class PremiumScheduleDto
{
    public Guid Id { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid? ProposalId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
}

public class PaymentRecordDto
{
    public Guid Id { get; set; }
    public Guid? PolicyId { get; set; }
    public Guid? ProposalId { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string ReceiptUrl { get; set; } = string.Empty;
    public string StripePaymentIntentId { get; set; } = string.Empty;
}

public class AgentCommissionDto
{
    public Guid Id { get; set; }
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public Guid PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class OverduePolicyDto
{
    public Guid? PolicyId { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public decimal AmountDue { get; set; }
    public int DaysOverdue { get; set; }
    public DateTime DueDate { get; set; }
}

public class PaymentSummaryDto
{
    public decimal TotalCollected { get; set; }
    public decimal Premiums { get; set; }
    public decimal ClaimsPaid { get; set; }
    public decimal NetInflow { get; set; }
    public int SuccessfulPayments { get; set; }
    public int FailedPayments { get; set; }
}

public record SavedCardDto(
    string PaymentMethodId,
    string Brand,
    string Last4,
    int ExpMonth,
    int ExpYear
);
