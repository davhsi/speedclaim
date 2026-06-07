using System.Threading.Tasks;
using Stripe;

namespace SpeedClaim.Api.Interfaces;

public interface IStripeWrapper
{
    Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options);
    Event ConstructEvent(string json, string stripeSignature, string webhookSecret);
}
