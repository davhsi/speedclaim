using System;

namespace SpeedClaim.Api.Dtos.Payments;

public record PaymentHistoryDto(
    Guid Id,
    Guid ReferencePolicyId,
    decimal Amount,
    string Currency,
    string Status,
    DateTimeOffset TransactionDate
);
