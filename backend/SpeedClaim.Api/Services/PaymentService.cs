using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Dtos.Payments;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using Stripe;

namespace SpeedClaim.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly string _webhookSecret;
    private readonly string _publishableKey;
    private readonly IStripeWrapper _stripeWrapper;

    public PaymentService(IUnitOfWork unitOfWork, IConfiguration configuration, IEmailService emailService, IStripeWrapper stripeWrapper)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _emailService = emailService;
        _stripeWrapper = stripeWrapper;
        _webhookSecret = _configuration.GetSection("Stripe")["WebhookSecret"] ?? string.Empty;
        _publishableKey = _configuration.GetSection("Stripe")["PublishableKey"] ?? string.Empty;
    }

    public async Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(Guid userId, CreatePaymentIntentRequest request)
    {
        var policy = await _unitOfWork.Policies.SingleOrDefaultAsync(p => p.Id == request.PolicyId && p.UserId == userId);
        
        if (policy == null)
            throw new ArgumentException("Policy not found or does not belong to the user.");

        if (policy.Status.ToUpper() != "PENDING")
            throw new ArgumentException("Payment can only be initiated for PENDING policies.");

        var amountInPaise = (long)(policy.PremiumAmount * 100); // Stripe expects smallest currency unit

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInPaise,
            Currency = "inr",
            Metadata = new System.Collections.Generic.Dictionary<string, string>
            {
                { "PolicyId", policy.Id.ToString() }
            }
        };

        var paymentIntent = await _stripeWrapper.CreatePaymentIntentAsync(options);

        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PolicyId = policy.Id,
            StripePaymentIntentId = paymentIntent.Id,
            Amount = policy.PremiumAmount,
            Currency = "INR",
            Status = "REQUIRES_PAYMENT",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PaymentTransactions.AddAsync(transaction);
        await _unitOfWork.CompleteAsync();

        return new CreatePaymentIntentResponse
        {
            ClientSecret = paymentIntent.ClientSecret,
            PublishableKey = _publishableKey,
            PaymentIntentId = paymentIntent.Id
        };
    }

    public async Task ProcessWebhookAsync(string json, string stripeSignature)
    {
        try
        {
            var stripeEvent = _stripeWrapper.ConstructEvent(json, stripeSignature, _webhookSecret);

            // Idempotency check using StripeEventId
            var existingEvent = await _unitOfWork.PaymentTransactions.SingleOrDefaultAsync(t => t.StripeEventId == stripeEvent.Id);
            if (existingEvent != null)
            {
                // We have already processed this event. Safely discard.
                return;
            }

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent == null) return;

                var transaction = await _unitOfWork.PaymentTransactions
                    .GetByIntentWithPolicyAsync(paymentIntent.Id);

                if (transaction != null)
                {
                    transaction.Status = "SUCCEEDED";
                    transaction.StripeEventId = stripeEvent.Id;
                    transaction.UpdatedAt = DateTime.UtcNow;

                    if (transaction.Policy != null && transaction.Policy.Status.ToUpper() == "PENDING")
                    {
                        transaction.Policy.Status = "ACTIVE";

                        var user = await _unitOfWork.Users.GetByIdAsync(transaction.Policy.UserId);
                        if (user != null)
                        {
                            var subject = "SpeedClaim Policy Activated";
                            var body = $"<h1>Policy Activated</h1><p>Dear {user.FullName}, your payment of {transaction.Amount} INR was successful. Your policy {transaction.Policy.PolicyNumber} is now ACTIVE.</p>";
                            await _emailService.SendEmailAsync(user.Email, user.FullName, subject, body);
                        }
                    }

                    await _unitOfWork.CompleteAsync();
                }
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent == null) return;

                var transaction = await _unitOfWork.PaymentTransactions
                    .SingleOrDefaultAsync(t => t.StripePaymentIntentId == paymentIntent.Id);

                if (transaction != null)
                {
                    transaction.Status = "FAILED";
                    transaction.StripeEventId = stripeEvent.Id;
                    transaction.UpdatedAt = DateTime.UtcNow;

                    await _unitOfWork.CompleteAsync();
                }
            }
        }
        catch (StripeException e)
        {
            throw new Exception($"Stripe webhook signature verification failed: {e.Message}");
        }
    }
}
