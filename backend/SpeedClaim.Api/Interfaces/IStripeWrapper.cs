using System.Threading.Tasks;
using Stripe;

namespace SpeedClaim.Api.Interfaces;

public interface IStripeWrapper
{
    Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options, RequestOptions? requestOptions = null);
    Task<Customer> CreateCustomerAsync(CustomerCreateOptions options, RequestOptions? requestOptions = null);
    Task<StripeList<PaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card");
    Task<Charge> GetChargeAsync(string chargeId);
    Event ConstructEvent(string json, string stripeSignature, string webhookSecret);
}
