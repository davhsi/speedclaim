using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    [Authorize(Roles = "Customer")]
    [HttpPost("pay/{scheduleId}")]
    public async Task<IActionResult> PayPremium(string scheduleId, [FromBody] SpeedClaim.Api.Dtos.Payments.CreatePaymentIntentRequest request)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.PayPremiumAsync(customerId, scheduleId, request);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("schedule/{policyId}")]
    public async Task<IActionResult> GetSchedule(string policyId)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.GetPremiumScheduleAsync(policyId, customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.GetMyPaymentHistoryAsync(customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("{paymentId}/receipt")]
    public async Task<IActionResult> DownloadReceipt(string paymentId)
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.DownloadReceiptAsync(paymentId, customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("methods")]
    public async Task<IActionResult> GetSavedPaymentMethods()
    {
        var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _financeService.GetSavedPaymentMethodsAsync(customerId);
        return Ok(result);
    }

    // --- Finance Officer Endpoints ---
    [Authorize(Roles = "FinanceOfficer,Admin")]
    [HttpGet("all-records")]
    public async Task<IActionResult> GetAllRecords()
    {
        var result = await _financeService.GetAllPaymentRecordsAsync();
        return Ok(result);
    }

    [Authorize(Roles = "FinanceOfficer")]
    [HttpPut("{paymentId}/reconcile")]
    public async Task<IActionResult> ReconcilePayment(string paymentId)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.ReconcilePaymentAsync(paymentId, officerId);
        return Ok();
    }

    [Authorize(Roles = "FinanceOfficer")]
    [HttpPost("{paymentId}/refund")]
    public async Task<IActionResult> RefundPayment(string paymentId)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.ProcessRefundAsync(paymentId, officerId);
        return Ok();
    }

    [Authorize(Roles = "FinanceOfficer")]
    [HttpPost("payout/claim/{claimId}")]
    public async Task<IActionResult> ProcessPayout(string claimId)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.ProcessClaimPayoutAsync(claimId, officerId);
        return Ok();
    }

    [Authorize(Roles = "FinanceOfficer")]
    [HttpPut("claims/{claimId}/settle")]
    public async Task<IActionResult> MarkClaimSettled(string claimId)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.MarkClaimFinanciallySettledAsync(claimId, officerId);
        return Ok();
    }

    [Authorize(Roles = "FinanceOfficer")]
    [HttpGet("commissions/pending")]
    public async Task<IActionResult> GetPendingCommissions()
    {
        var result = await _financeService.GetPendingCommissionsAsync();
        return Ok(result);
    }

    [Authorize(Roles = "FinanceOfficer")]
    [HttpPost("commissions/{id}/approve")]
    public async Task<IActionResult> ApproveCommission(string id)
    {
        var officerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _financeService.ApproveAndPayCommissionAsync(id, officerId);
        return Ok();
    }

    // --- Reports ---
    [Authorize(Roles = "FinanceOfficer,Admin")]
    [HttpGet("reports/overdue")]
    public async Task<IActionResult> GetOverduePolicies()
    {
        var result = await _financeService.GetOverduePoliciesAsync();
        return Ok(result);
    }

    [Authorize(Roles = "FinanceOfficer,Admin")]
    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetPremiumCollectionSummary([FromQuery] string period)
    {
        var result = await _financeService.GetPremiumCollectionSummaryAsync(period);
        return Ok(result);
    }

    [Authorize(Roles = "FinanceOfficer,Admin")]
    [HttpGet("reports/export")]
    public async Task<IActionResult> ExportPaymentReports()
    {
        var bytes = await _financeService.ExportPaymentReportsAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PaymentReport.xlsx");
    }

    // --- Webhooks ---
    [AllowAnonymous]
    [HttpPost("webhook")]
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
