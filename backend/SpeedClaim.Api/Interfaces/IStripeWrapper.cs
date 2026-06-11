using System.Threading.Tasks;
using Stripe;

namespace SpeedClaim.Api.Interfaces;

public interface IStripeWrapper
{
    Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options);
    Task<Customer> CreateCustomerAsync(CustomerCreateOptions options);
    Task<StripeList<PaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card");
    Event ConstructEvent(string json, string stripeSignature, string webhookSecret);
}
