namespace SpeedClaim.Api.Dtos.Payments;

public class CreatePaymentIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
}
