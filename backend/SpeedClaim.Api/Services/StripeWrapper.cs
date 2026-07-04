using System.Threading.Tasks;
using Stripe;
using SpeedClaim.Api.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace SpeedClaim.Api.Services;

[ExcludeFromCodeCoverage]
public class StripeWrapper : IStripeWrapper
{
    public async Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options, RequestOptions? requestOptions = null)
    {
        var service = new PaymentIntentService();
        return await service.CreateAsync(options, requestOptions);
    }

    public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
    {
        var service = new PaymentIntentService();
        return await service.GetAsync(paymentIntentId);
    }

    public async Task<Customer> CreateCustomerAsync(CustomerCreateOptions options, RequestOptions? requestOptions = null)
    {
        var service = new CustomerService();
        return await service.CreateAsync(options, requestOptions);
    }

    public async Task<StripeList<PaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card")
    {
        var service = new PaymentMethodService();
        return await service.ListAsync(new PaymentMethodListOptions { Customer = stripeCustomerId, Type = type });
    }

    public async Task<Charge> GetChargeAsync(string chargeId)
    {
        var service = new ChargeService();
        return await service.GetAsync(chargeId);
    }

    public async Task<Refund> CreateRefundAsync(RefundCreateOptions options, RequestOptions? requestOptions = null)
    {
        var service = new RefundService();
        return await service.CreateAsync(options, requestOptions);
    }

    public Event ConstructEvent(string json, string stripeSignature, string webhookSecret)
    {
        return EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
    }
}
