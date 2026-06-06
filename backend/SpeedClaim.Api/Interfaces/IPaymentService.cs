using System;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Payments;

namespace SpeedClaim.Api.Interfaces;

public interface IPaymentService
{
    Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(Guid userId, CreatePaymentIntentRequest request);
    Task ProcessWebhookAsync(string json, string stripeSignature);
}
