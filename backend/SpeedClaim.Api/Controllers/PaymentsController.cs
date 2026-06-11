using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SpeedClaim.Api.Dtos.Financial;
using SpeedClaim.Api.Dtos.Payments;
using SpeedClaim.Api.Interfaces;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize]
public class PaymentsController : BaseApiController
{
    private readonly IFinanceService _financeService;
    private readonly IStripeWrapper _stripeWrapper;
    private readonly IConfiguration _config;

    public PaymentsController(IFinanceService financeService, IStripeWrapper stripeWrapper, IConfiguration config)
    {
        _financeService = financeService;
        _stripeWrapper = stripeWrapper;
        _config = config;
    }

    // --- Customer Endpoints ---

    /// <summary>Create a Stripe PaymentIntent for a premium installment</summary>
    /// <remarks>Returns a client_secret to complete payment on the frontend. Stripe customer is created automatically if not present.</remarks>
    /// <param name="scheduleId">Premium schedule installment ID</param>
    [Authorize(Roles = "Customer")]
    [HttpPost("pay/{scheduleId}")]
    [ProducesResponseType(typeof(CreatePaymentIntentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> PayPremium(string scheduleId, [FromBody] SpeedClaim.Api.Dtos.Payments.CreatePaymentIntentRequest request)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.PayPremiumAsync(customerId, scheduleId, request);
        return Ok(result);
    }

    /// <summary>Get the premium installment schedule for a policy</summary>
    /// <param name="policyId">Policy ID</param>
    [Authorize(Roles = "Customer")]
    [HttpGet("schedule/{policyId}")]
    [ProducesResponseType(typeof(IEnumerable<PremiumScheduleDto>), 200)]
    public async Task<IActionResult> GetSchedule(string policyId)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.GetPremiumScheduleAsync(policyId, customerId);
        return Ok(result);
    }

    /// <summary>Get all premium payment transactions for the authenticated customer</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<PaymentRecordDto>), 200)]
    public async Task<IActionResult> GetHistory()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.GetMyPaymentHistoryAsync(customerId);
        return Ok(result);
    }

    /// <summary>Get payment receipt details for a completed transaction</summary>
    /// <remarks>Only available for payments with Paid status.</remarks>
    /// <param name="paymentId">Payment ID</param>
    [Authorize(Roles = "Customer")]
    [HttpGet("{paymentId}/receipt")]
    [ProducesResponseType(typeof(PaymentRecordDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> DownloadReceipt(string paymentId)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.DownloadReceiptAsync(paymentId, customerId);
        return Ok(result);
    }

    /// <summary>Get saved Stripe payment methods (cards) for the authenticated customer</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("methods")]
    [ProducesResponseType(typeof(IEnumerable<SavedCardDto>), 200)]
    public async Task<IActionResult> GetSavedPaymentMethods()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.GetSavedPaymentMethodsAsync(customerId);
        return Ok(result);
    }

    // --- Finance Officer Endpoints ---

    /// <summary>Get all payment records across all customers</summary>
    [Authorize(Roles = "FinanceOfficer,Admin")]
    [HttpGet("all-records")]
    [ProducesResponseType(typeof(IEnumerable<PaymentRecordDto>), 200)]
    public async Task<IActionResult> GetAllRecords()
    {
        var result = await _financeService.GetAllPaymentRecordsAsync();
        return Ok(result);
    }

    /// <summary>Manually mark a payment as reconciled (Paid)</summary>
    /// <remarks>Also activates the associated policy if it is in Pending status.</remarks>
    /// <param name="paymentId">Payment ID</param>
    [Authorize(Roles = "FinanceOfficer")]
    [HttpPut("{paymentId}/reconcile")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ReconcilePayment(string paymentId)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.ReconcilePaymentAsync(paymentId, officerId);
        return Ok();
    }

    /// <summary>Process a refund for a payment and revert the associated schedule to Overdue</summary>
    /// <param name="paymentId">Payment ID</param>
    [Authorize(Roles = "FinanceOfficer")]
    [HttpPost("{paymentId}/refund")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RefundPayment(string paymentId)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.ProcessRefundAsync(paymentId, officerId);
        return Ok();
    }

    /// <summary>Initiate a Stripe PaymentIntent as a claim payout simulation</summary>
    /// <remarks>Claim must be in Approved status. Marks claim as Settled after payout is initiated.</remarks>
    /// <param name="claimId">Claim ID</param>
    [Authorize(Roles = "FinanceOfficer")]
    [HttpPost("payout/claim/{claimId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> ProcessPayout(string claimId)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.ProcessClaimPayoutAsync(claimId, officerId);
        return Ok();
    }

    /// <summary>Mark a claim as financially settled without a Stripe payout</summary>
    /// <param name="claimId">Claim ID</param>
    [Authorize(Roles = "FinanceOfficer")]
    [HttpPut("claims/{claimId}/settle")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> MarkClaimSettled(string claimId)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.MarkClaimFinanciallySettledAsync(claimId, officerId);
        return Ok();
    }

    /// <summary>Get all pending agent commissions awaiting approval</summary>
    [Authorize(Roles = "FinanceOfficer")]
    [HttpGet("commissions/pending")]
    [ProducesResponseType(typeof(IEnumerable<AgentCommissionDto>), 200)]
    public async Task<IActionResult> GetPendingCommissions()
    {
        var result = await _financeService.GetPendingCommissionsAsync();
        return Ok(result);
    }

    /// <summary>Approve and mark an agent commission as paid</summary>
    /// <param name="id">Commission ID</param>
    [Authorize(Roles = "FinanceOfficer")]
    [HttpPost("commissions/{id}/approve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApproveCommission(string id)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.ApproveAndPayCommissionAsync(id, officerId);
        return Ok();
    }

    // --- Reports ---

    /// <summary>Get all premium schedules currently in Overdue status</summary>
    [Authorize(Roles = "FinanceOfficer,Admin")]
    [HttpGet("reports/overdue")]
    [ProducesResponseType(typeof(IEnumerable<PremiumScheduleDto>), 200)]
    public async Task<IActionResult> GetOverduePolicies()
    {
        var result = await _financeService.GetOverduePoliciesAsync();
        return Ok(result);
    }

    /// <summary>Get a summary of premium collections — total collected, successful and failed payment counts</summary>
    [Authorize(Roles = "FinanceOfficer,Admin")]
    [HttpGet("reports/summary")]
    [ProducesResponseType(typeof(PaymentSummaryDto), 200)]
    public async Task<IActionResult> GetPremiumCollectionSummary([FromQuery] string period)
    {
        var result = await _financeService.GetPremiumCollectionSummaryAsync(period);
        return Ok(result);
    }

    /// <summary>Export all payment records as an Excel (.xlsx) file</summary>
    [Authorize(Roles = "FinanceOfficer,Admin")]
    [HttpGet("reports/export")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> ExportPaymentReports()
    {
        var bytes = await _financeService.ExportPaymentReportsAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PaymentReport.xlsx");
    }

    // --- Webhooks ---

    /// <summary>Stripe webhook receiver — handles payment_intent.succeeded events to auto-reconcile payments</summary>
    /// <remarks>This endpoint is called by Stripe. Do not call directly. Validates the Stripe-Signature header.</remarks>
    [AllowAnonymous]
    [HttpPost("webhook")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
        var webhookSecret = _config["Stripe:WebhookSecret"] ?? string.Empty;

        Stripe.Event stripeEvent;
        try
        {
            stripeEvent = _stripeWrapper.ConstructEvent(json, stripeSignature, webhookSecret);
        }
        catch (Stripe.StripeException)
        {
            return BadRequest("Invalid Stripe webhook signature.");
        }

        if (stripeEvent.Type == "payment_intent.succeeded")
        {
            var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
            if (paymentIntent != null)
            {
                await _financeService.ReconcileByStripeIntentAsync(paymentIntent.Id);
            }
        }

        return Ok();
    }
}
