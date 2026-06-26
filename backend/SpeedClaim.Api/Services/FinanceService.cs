using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ClosedXML.Excel;

using Microsoft.Extensions.Logging;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Dtos.Financial;
using SpeedClaim.Api.Dtos.Payments;
using Stripe;

namespace SpeedClaim.Api.Services;

public class FinanceService : IFinanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeWrapper _stripeWrapper;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notifications;
    private readonly ILogger<FinanceService> _logger;

    public FinanceService(IUnitOfWork unitOfWork, IStripeWrapper stripeWrapper, IConfiguration config, IEmailService emailService, INotificationService notifications, ILogger<FinanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _stripeWrapper = stripeWrapper;
        _config = config;
        _emailService = emailService;
        _notifications = notifications;
        _logger = logger;
    }

    private async Task<Models.Customer> ResolveCustomerAsync(Guid userId)
    {
        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) throw new NotFoundException("Customer not found.");
        return customer;
    }

    public async Task<CreatePaymentIntentResponse> PayPremiumAsync(string customerId, string scheduleId, CreatePaymentIntentRequest request)
    {
        if (!Guid.TryParse(customerId, out var cid) || !Guid.TryParse(scheduleId, out var sid))
            throw new ValidationException("Invalid IDs.");

        var schedule = await _unitOfWork.PremiumSchedules.GetByIdAsync(sid);
        if (schedule == null) throw new NotFoundException("Schedule not found.");

        if (schedule.Status == PremiumScheduleStatus.Paid)
            throw new ConflictException("Schedule is already paid.");

        var customerRecord = await ResolveCustomerAsync(cid);

        var stripeCustomerRecord = await _unitOfWork.StripeCustomers.FirstOrDefaultAsync(sc => sc.UserId == customerRecord.UserId);
        if (stripeCustomerRecord == null)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(customerRecord.UserId);
            if (user == null) throw new NotFoundException("User not found.");

            var stripeCustomer = await _stripeWrapper.CreateCustomerAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = $"{user.FirstName} {user.LastName}",
                Metadata = new Dictionary<string, string> { { "userId", user.Id.ToString() } }
            });

            stripeCustomerRecord = new Models.StripeCustomer
            {
                Id = Guid.NewGuid(),
                UserId = customerRecord.UserId,
                StripeCustomerId = stripeCustomer.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.StripeCustomers.AddAsync(stripeCustomerRecord);
        }

        // Create PaymentIntent attached to the customer so cards are saved for renewals
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(schedule.Amount * 100), // Stripe expects cents
            Currency = "usd",
            Customer = stripeCustomerRecord.StripeCustomerId,
            SetupFutureUsage = "off_session",
            Metadata = new Dictionary<string, string>
            {
                { "scheduleId", schedule.Id.ToString() },
                { "policyId", request.PolicyId.ToString() }
            }
        };

        var stripeRequestOptions = new RequestOptions
        {
            IdempotencyKey = $"pay-premium-{schedule.Id}"
        };
        var intent = await _stripeWrapper.CreatePaymentIntentAsync(options, stripeRequestOptions);

        var payment = new PremiumPayment
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            PolicyId = schedule.PolicyId,
            ProposalId = schedule.ProposalId,
            CustomerId = customerRecord.Id,
            Amount = schedule.Amount,
            Currency = "USD",
            PaymentType = schedule.InstallmentNumber == 1 ? PaymentType.FirstPremium : PaymentType.Renewal,
            Status = PaymentStatus.Pending,
            StripePaymentIntentId = intent.Id
        };
        await _unitOfWork.PremiumPayments.AddAsync(payment);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Payment intent created for Customer {CustomerId}, Schedule {ScheduleId}, Amount {Amount}", customerId, scheduleId, schedule.Amount);
        return new CreatePaymentIntentResponse
        {
            ClientSecret = intent.ClientSecret,
            PaymentIntentId = intent.Id,
            PublishableKey = _config["Stripe:PublishableKey"] ?? string.Empty
        };
    }

    public async Task<IEnumerable<PremiumScheduleDto>> GetPremiumScheduleAsync(string policyId, string customerId)
    {
        if (!Guid.TryParse(policyId, out var pid)) throw new ValidationException("Invalid Policy ID");

        var schedules = await _unitOfWork.PremiumSchedules.FindAsync(s => s.PolicyId == pid);
        return schedules.Select(s => new PremiumScheduleDto
        {
            Id = s.Id,
            PolicyId = s.PolicyId,
            ProposalId = s.ProposalId,
            InstallmentNumber = s.InstallmentNumber,
            AmountDue = s.Amount,
            DueDate = s.DueDate,
            Status = s.Status.ToString(),
            PaymentId = s.PaymentId
        });
    }

    public async Task<IEnumerable<PaymentRecordDto>> GetMyPaymentHistoryAsync(string customerId)
    {
        if (!Guid.TryParse(customerId, out var cid)) throw new ValidationException("Invalid Customer ID");

        var customerRecord = await ResolveCustomerAsync(cid);
        var payments = await _unitOfWork.PremiumPayments.FindAsync(p => p.CustomerId == customerRecord.Id);
        return await MapPaymentRecordsAsync(payments);
    }

    public async Task<PaymentRecordDto> DownloadReceiptAsync(string paymentId, string customerId)
    {
        if (!Guid.TryParse(paymentId, out var pid)) throw new ValidationException("Invalid Payment ID");
        if (!Guid.TryParse(customerId, out var cid)) throw new ValidationException("Invalid Customer ID");

        var customerRecord = await ResolveCustomerAsync(cid);
        var payment = await _unitOfWork.PremiumPayments.GetByIdAsync(pid);
        if (payment == null || payment.CustomerId != customerRecord.Id)
            throw new NotFoundException("Payment not found or access denied.");

        if (payment.Status != PaymentStatus.Paid)
            throw new UnprocessableException("Receipt is only available for completed payments.");

        return (await MapPaymentRecordsAsync(new[] { payment })).Single();
    }

    public async Task<IEnumerable<SavedCardDto>> GetSavedPaymentMethodsAsync(string customerId)
    {
        if (!Guid.TryParse(customerId, out var cid)) throw new ValidationException("Invalid Customer ID");

        var customerRecord = await ResolveCustomerAsync(cid);

        var stripeCustomerRecord = await _unitOfWork.StripeCustomers.FirstOrDefaultAsync(sc => sc.UserId == customerRecord.UserId);
        if (stripeCustomerRecord == null) return Enumerable.Empty<SavedCardDto>();

        var methods = await _stripeWrapper.ListPaymentMethodsAsync(stripeCustomerRecord.StripeCustomerId);
        return methods.Select(pm => new SavedCardDto(
            pm.Id,
            pm.Card.Brand,
            pm.Card.Last4,
            (int)pm.Card.ExpMonth,
            (int)pm.Card.ExpYear));
    }

    public async Task<IEnumerable<PaymentRecordDto>> GetAllPaymentRecordsAsync()
    {
        var payments = await _unitOfWork.PremiumPayments.GetAllAsync();
        return await MapPaymentRecordsAsync(payments);
    }

    private async Task<IEnumerable<PaymentRecordDto>> MapPaymentRecordsAsync(IEnumerable<PremiumPayment> payments)
    {
        var paymentList = payments.ToList();
        var policyIds = paymentList.Where(p => p.PolicyId.HasValue).Select(p => p.PolicyId!.Value).Distinct().ToList();
        var customerIds = paymentList.Select(p => p.CustomerId).Distinct().ToList();

        var policies = policyIds.Count == 0
            ? new List<Policy>()
            : (await _unitOfWork.Policies.FindAsync(p => policyIds.Contains(p.Id))).ToList();
        var customers = customerIds.Count == 0
            ? new List<SpeedClaim.Api.Models.Customer>()
            : (await _unitOfWork.Customers.FindAsync(c => customerIds.Contains(c.Id))).ToList();
        var userIds = customers.Select(c => c.UserId).Distinct().ToList();
        var users = userIds.Count == 0
            ? new List<User>()
            : (await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id))).ToList();

        var policyMap = policies.ToDictionary(p => p.Id);
        var customerMap = customers.ToDictionary(c => c.Id);
        var userMap = users.ToDictionary(u => u.Id);

        return paymentList.Select(p =>
        {
            customerMap.TryGetValue(p.CustomerId, out var customer);
            var user = customer != null && userMap.TryGetValue(customer.UserId, out var foundUser) ? foundUser : null;
            var customerName = user == null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();
            var policyNumber = p.PolicyId.HasValue && policyMap.TryGetValue(p.PolicyId.Value, out var policy)
                ? policy.PolicyNumber
                : string.Empty;

            return new PaymentRecordDto
            {
                Id = p.Id,
                PolicyId = p.PolicyId,
                ProposalId = p.ProposalId,
                CustomerId = p.CustomerId,
                CustomerName = customerName,
                PolicyNumber = policyNumber,
                Amount = p.Amount,
                Currency = p.Currency,
                PaymentType = p.PaymentType.ToString(),
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt,
                ReceiptUrl = p.ReceiptUrl,
                StripePaymentIntentId = p.StripePaymentIntentId
            };
        });
    }

    public async Task ReconcilePaymentAsync(string paymentId, string financeOfficerId)
    {
        if (!Guid.TryParse(paymentId, out var pid)) throw new ValidationException("Invalid Payment ID");
        var payment = await _unitOfWork.PremiumPayments.GetByIdAsync(pid);
        if (payment == null) throw new NotFoundException("Payment not found");

        if (payment.Status != PaymentStatus.Paid)
        {
            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTimeOffset.UtcNow;
            
            if (payment.ScheduleId.HasValue)
            {
                var schedule = await _unitOfWork.PremiumSchedules.GetByIdAsync(payment.ScheduleId.Value);
                if (schedule != null)
                {
                    schedule.Status = PremiumScheduleStatus.Paid;
                    schedule.PaymentId = payment.Id;

                    if (schedule.PolicyId.HasValue)
                    {
                        var policy = await _unitOfWork.Policies.GetByIdAsync(schedule.PolicyId.Value);
                        if (policy != null && policy.Status == PolicyStatus.Pending)
                        {
                            policy.Status = PolicyStatus.Active;
                            policy.IssuedAt = DateTimeOffset.UtcNow;

                            var customer = await _unitOfWork.Customers.GetByIdAsync(policy.CustomerId);
                            if (customer != null)
                            {
                                var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
                                if (user != null)
                                {
                                    var productRepo = _unitOfWork.InsuranceProducts;
                                    var product = productRepo == null ? null : await productRepo.GetByIdAsync(policy.ProductId);
                                    var productName = product?.ProductName ?? policy.PolicyType.ToString();
                                    var customerName = $"{user.FirstName} {user.LastName}".Trim();
                                    var certificate = PolicyDocumentGenerator.GenerateCertificatePdf(policy, customerName, productName);
                                    var attachment = new EmailAttachment(
                                        $"{policy.PolicyNumber}-certificate.pdf",
                                        PolicyDocumentGenerator.ContentType,
                                        certificate);

                                    await _emailService.SendEmailAsync(
                                        user.Email,
                                        $"Your SpeedClaim policy is active - {policy.PolicyNumber}",
                                        BuildPolicyActivatedEmail(user, policy, productName),
                                        attachment);
                                    await _notifications.CreateAsync(
                                        user.Id,
                                        "Policy Activated",
                                        $"Your policy {policy.PolicyNumber} is now active.",
                                        "policy"
                                    );
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Payment {PaymentId} reconciled as Paid", payment.Id);
            await _unitOfWork.CompleteAsync();
        }
    }

    private static string BuildPolicyActivatedEmail(User user, Policy policy, string productName)
    {
        static string Enc(string value) => WebUtility.HtmlEncode(value);
        var firstName = Enc(user.FirstName);
        var policyNumber = Enc(policy.PolicyNumber);
        var product = Enc(productName);
        var status = Enc(policy.Status.ToString());
        var frequency = Enc(policy.PaymentFrequency);
        var year = DateTime.UtcNow.Year;

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Policy Activated</title>
</head>
<body style=""margin:0; padding:0; background-color:#F4F7FA; font-family:Arial, Helvetica, sans-serif; color:#1A2230;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F4F7FA; padding:28px 12px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""640"" cellpadding=""0"" cellspacing=""0"" style=""max-width:640px; width:100%; background:#ffffff; border:1px solid #E2E6EB; border-radius:14px; overflow:hidden;"">
                    <tr>
                        <td style=""background:#0F6E8C; padding:24px 30px;"">
                            <div style=""font-size:22px; font-weight:700; color:#ffffff;"">SpeedClaim</div>
                            <div style=""font-size:13px; color:#D8EEF5; margin-top:4px;"">Insurance policy services</div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:30px;"">
                            <p style=""margin:0 0 10px; font-size:14px; color:#1F9D6B; font-weight:700;"">Policy activated</p>
                            <h1 style=""margin:0 0 14px; font-size:25px; line-height:1.25; color:#1A2230;"">Your cover is now active</h1>
                            <p style=""margin:0 0 24px; font-size:15px; line-height:1.65; color:#3C4654;"">Dear {firstName}, your policy has been activated successfully. A PDF copy of your policy certificate is attached to this email for your records.</p>

                            <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse; border:1px solid #E2E6EB; border-radius:10px; overflow:hidden;"">
                                <tr>
                                    <td style=""padding:13px 16px; background:#F7F9FA; font-size:12px; color:#6B7685; width:38%;"">Policy number</td>
                                    <td style=""padding:13px 16px; font-size:14px; font-weight:700; color:#1A2230;"">{policyNumber}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:13px 16px; background:#F7F9FA; font-size:12px; color:#6B7685;"">Product</td>
                                    <td style=""padding:13px 16px; font-size:14px; color:#1A2230;"">{product}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:13px 16px; background:#F7F9FA; font-size:12px; color:#6B7685;"">Coverage amount</td>
                                    <td style=""padding:13px 16px; font-size:14px; color:#1A2230;"">{policy.SumAssured:0.00} USD</td>
                                </tr>
                                <tr>
                                    <td style=""padding:13px 16px; background:#F7F9FA; font-size:12px; color:#6B7685;"">Premium</td>
                                    <td style=""padding:13px 16px; font-size:14px; color:#1A2230;"">{policy.PremiumAmount:0.00} USD / {frequency}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:13px 16px; background:#F7F9FA; font-size:12px; color:#6B7685;"">Policy period</td>
                                    <td style=""padding:13px 16px; font-size:14px; color:#1A2230;"">{policy.StartDate:dd MMM yyyy} - {policy.EndDate:dd MMM yyyy}</td>
                                </tr>
                                <tr>
                                    <td style=""padding:13px 16px; background:#F7F9FA; font-size:12px; color:#6B7685;"">Status</td>
                                    <td style=""padding:13px 16px; font-size:14px; font-weight:700; color:#1F9D6B;"">{status}</td>
                                </tr>
                            </table>

                            <p style=""margin:24px 0 0; font-size:13px; line-height:1.55; color:#6B7685;"">Please review the attached certificate and keep it with your insurance records. Contact SpeedClaim support if any detail looks incorrect.</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:18px 30px; background:#F7F9FA; border-top:1px solid #E2E6EB;"">
                            <p style=""margin:0; font-size:12px; line-height:1.5; color:#6B7685;"">This is a system-generated message from SpeedClaim. © {year} SpeedClaim.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    public async Task ReconcileByStripeIntentAsync(string paymentIntentId, string? chargeId = null)
    {
        var payment = await _unitOfWork.PremiumPayments.GetByIntentWithScheduleAsync(paymentIntentId);
        if (payment == null) return; // idempotent — if no matching payment, ignore

        await ReconcilePaymentAsync(payment.Id.ToString(), "system-webhook");

        // Best-effort: capture the Stripe receipt URL + charge id from the charge.
        // Never let this fail the webhook — reconciliation above is already persisted.
        if (!string.IsNullOrEmpty(chargeId))
        {
            try
            {
                var charge = await _stripeWrapper.GetChargeAsync(chargeId);
                if (charge != null && !string.IsNullOrEmpty(charge.ReceiptUrl))
                {
                    payment.ReceiptUrl = charge.ReceiptUrl;
                    payment.StripeChargeId = charge.Id;
                    await _unitOfWork.CompleteAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch Stripe receipt URL for payment {PaymentId}", payment.Id);
            }
        }
    }

    public async Task ProcessRefundAsync(string paymentId, string financeOfficerId)
    {
        if (!Guid.TryParse(paymentId, out var pid)) throw new ValidationException("Invalid Payment ID");
        var payment = await _unitOfWork.PremiumPayments.GetByIdAsync(pid);
        if (payment == null) throw new NotFoundException("Payment not found");

        payment.Status = PaymentStatus.Refunded;
        
        if (payment.ScheduleId.HasValue)
        {
            var schedule = await _unitOfWork.PremiumSchedules.GetByIdAsync(payment.ScheduleId.Value);
            if (schedule != null)
            {
                schedule.Status = PremiumScheduleStatus.Overdue; // Or Unpaid depending on business logic
            }
        }
        await _unitOfWork.CompleteAsync();
    }

    public async Task ProcessClaimPayoutAsync(string claimId, string financeOfficerId)
    {
        if (!Guid.TryParse(claimId, out var cId)) throw new ValidationException("Invalid Claim ID");
        var claim = await _unitOfWork.Claims.GetByIdAsync(cId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        if (claim.Status != ClaimStatus.Approved)
            throw new UnprocessableException("Claim must be approved before payout.");

        // In sandbox mode, we create a PaymentIntent to the customer as a payout simulation.
        // In production, this would use Stripe Transfers or Payouts API.
        var payoutAmount = (long)((claim.ClaimAmountApproved ?? 0) * 100);
        if (payoutAmount <= 0)
            throw new ValidationException("Approved claim amount must be greater than zero.");

        var options = new PaymentIntentCreateOptions
        {
            Amount = payoutAmount,
            Currency = "usd",
            Metadata = new Dictionary<string, string>
            {
                { "type", "claim_payout" },
                { "claimId", cId.ToString() },
                { "financeOfficerId", financeOfficerId }
            },
            Description = $"Claim payout for claim {claim.ClaimNumber}"
        };

        var stripeRequestOptions = new RequestOptions
        {
            IdempotencyKey = $"claim-payout-{cId}"
        };
        var intent = await _stripeWrapper.CreatePaymentIntentAsync(options, stripeRequestOptions);
        _logger.LogInformation("Claim payout initiated for Claim {ClaimId}, Amount {Amount}, IntentId {IntentId}", cId, payoutAmount / 100m, intent.Id);

        // Mark claim as Settled and log the payout intent
        claim.Status = ClaimStatus.Settled;
        claim.SettlementDate = DateTime.UtcNow;
        claim.UpdatedAt = DateTimeOffset.UtcNow;

        // Record a status history entry
        await _unitOfWork.ClaimStatusHistories.AddAsync(new ClaimStatusHistory
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            OldStatus = ClaimStatus.Approved,
            NewStatus = ClaimStatus.Settled,
            ChangedById = Guid.Parse(financeOfficerId),
            Notes = $"Payout initiated via Stripe PaymentIntent: {intent.Id}",
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    public async Task MarkClaimFinanciallySettledAsync(string claimId, string financeOfficerId)
    {
        if (!Guid.TryParse(claimId, out var cId)) throw new ValidationException("Invalid Claim ID");
        var claim = await _unitOfWork.Claims.GetByIdAsync(cId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        if (claim.Status == ClaimStatus.Settled)
            throw new ConflictException("Claim is already marked as settled.");

        claim.Status = ClaimStatus.Settled;
        claim.SettlementDate = DateTime.UtcNow;
        claim.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.ClaimStatusHistories.AddAsync(new ClaimStatusHistory
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            OldStatus = ClaimStatus.Approved,
            NewStatus = ClaimStatus.Settled,
            ChangedById = Guid.Parse(financeOfficerId),
            Notes = "Marked as financially settled by Finance Officer.",
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<AgentCommissionDto>> GetPendingCommissionsAsync()
    {
        var commissions = await _unitOfWork.AgentCommissions.FindAsync(c => c.Status == "PENDING");
        return commissions.Select(c => new AgentCommissionDto
        {
            Id = c.Id,
            AgentId = c.AgentId,
            PolicyId = c.PolicyId,
            CommissionAmount = c.CommissionAmount,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
            PaidAt = c.PaidAt
        });
    }

    public async Task ApproveAndPayCommissionAsync(string commissionId, string financeOfficerId)
    {
        if (!Guid.TryParse(commissionId, out var cid)) throw new ValidationException("Invalid Commission ID");
        var commission = await _unitOfWork.AgentCommissions.GetByIdAsync(cid);
        if (commission == null) throw new NotFoundException("Commission not found");

        commission.Status = "PAID";
        commission.PaidAt = DateTimeOffset.UtcNow;
        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<PremiumScheduleDto>> GetOverduePoliciesAsync()
    {
        var overdueSchedules = await _unitOfWork.PremiumSchedules.FindAsync(s => s.Status == PremiumScheduleStatus.Overdue);
        return overdueSchedules.Select(s => new PremiumScheduleDto
        {
            Id = s.Id,
            PolicyId = s.PolicyId,
            ProposalId = s.ProposalId,
            InstallmentNumber = s.InstallmentNumber,
            AmountDue = s.Amount,
            DueDate = s.DueDate,
            Status = s.Status.ToString(),
            PaymentId = s.PaymentId
        });
    }

    public async Task<PaymentSummaryDto> GetPremiumCollectionSummaryAsync(string period)
    {
        var payments = await _unitOfWork.PremiumPayments.GetAllAsync();
        return new PaymentSummaryDto
        {
            TotalCollected = payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount),
            SuccessfulPayments = payments.Count(p => p.Status == PaymentStatus.Paid),
            FailedPayments = payments.Count(p => p.Status == PaymentStatus.Failed)
        };
    }

    public async Task<byte[]> ExportPaymentReportsAsync()
    {
        var payments = await _unitOfWork.PremiumPayments.GetAllAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Payment Report");

        // Header row
        ws.Cell(1, 1).Value = "Payment ID";
        ws.Cell(1, 2).Value = "Policy ID";
        ws.Cell(1, 3).Value = "Proposal ID";
        ws.Cell(1, 4).Value = "Customer ID";
        ws.Cell(1, 5).Value = "Amount";
        ws.Cell(1, 6).Value = "Currency";
        ws.Cell(1, 7).Value = "Payment Type";
        ws.Cell(1, 8).Value = "Status";
        ws.Cell(1, 9).Value = "Stripe Intent ID";
        ws.Cell(1, 10).Value = "Paid At";
        ws.Cell(1, 11).Value = "Created At";

        // Style header
        var headerRow = ws.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.DarkBlue;
        headerRow.Style.Font.FontColor = XLColor.White;

        // Data rows
        int row = 2;
        foreach (var p in payments)
        {
            ws.Cell(row, 1).Value = p.Id.ToString();
            ws.Cell(row, 2).Value = p.PolicyId?.ToString() ?? "";
            ws.Cell(row, 3).Value = p.ProposalId?.ToString() ?? "";
            ws.Cell(row, 4).Value = p.CustomerId.ToString();
            ws.Cell(row, 5).Value = p.Amount;
            ws.Cell(row, 6).Value = p.Currency;
            ws.Cell(row, 7).Value = p.PaymentType.ToString();
            ws.Cell(row, 8).Value = p.Status.ToString();
            ws.Cell(row, 9).Value = p.StripePaymentIntentId;
            ws.Cell(row, 10).Value = p.PaidAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
            ws.Cell(row, 11).Value = p.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
