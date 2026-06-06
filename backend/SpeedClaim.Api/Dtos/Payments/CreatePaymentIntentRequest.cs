using System;

namespace SpeedClaim.Api.Dtos.Payments;

public class CreatePaymentIntentRequest
{
    public Guid PolicyId { get; set; }
}
