using System;

namespace SpeedClaim.Api.Dtos.Financial;

public class PremiumScheduleDto
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public int InstallmentNumber { get; set; }
    public decimal AmountDue { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
}

public class PaymentStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public Guid? ChangedById { get; set; }
    public string? Remarks { get; set; }
    public DateTime ChangedAt { get; set; }
}
