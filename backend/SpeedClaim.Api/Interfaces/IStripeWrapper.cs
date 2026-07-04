using System.Threading.Tasks;
using Stripe;

namespace SpeedClaim.Api.Interfaces;

public interface IStripeWrapper
{
    Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options, RequestOptions? requestOptions = null);
    Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId);
    Task<Customer> CreateCustomerAsync(CustomerCreateOptions options, RequestOptions? requestOptions = null);
    Task<StripeList<PaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card");
    Task<Charge> GetChargeAsync(string chargeId);
    Task<Refund> CreateRefundAsync(RefundCreateOptions options, RequestOptions? requestOptions = null);
    Event ConstructEvent(string json, string stripeSignature, string webhookSecret);
}
