using System.Threading.Tasks;
using Stripe;
using SpeedClaim.Api.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace SpeedClaim.Api.Services;

[ExcludeFromCodeCoverage]
public class StripeWrapper : IStripeWrapper
{
    public async Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options)
    {
        var service = new PaymentIntentService();
        return await service.CreateAsync(options);
    }

    public async Task<Customer> CreateCustomerAsync(CustomerCreateOptions options)
    {
        var service = new CustomerService();
        return await service.CreateAsync(options);
    }

    public async Task<StripeList<PaymentMethod>> ListPaymentMethodsAsync(string stripeCustomerId, string type = "card")
    {
        var service = new PaymentMethodService();
        return await service.ListAsync(new PaymentMethodListOptions { Customer = stripeCustomerId, Type = type });
    }

    public Event ConstructEvent(string json, string stripeSignature, string webhookSecret)
    {
        return EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
    }
}
