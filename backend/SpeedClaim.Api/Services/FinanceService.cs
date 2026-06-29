using System;
using System.Collections.Generic;
using System.Globalization;
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
        var policy = schedule.PolicyId.HasValue
            ? await _unitOfWork.Policies.GetByIdAsync(schedule.PolicyId.Value)
            : null;
        if (policy == null || policy.CustomerId != customerRecord.Id || policy.Id != request.PolicyId)
            throw new ForbiddenException("Access denied to this premium schedule.");

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
        if (!Guid.TryParse(customerId, out var cid)) throw new ValidationException("Invalid Customer ID");

        var customerRecord = await ResolveCustomerAsync(cid);
        var policy = await _unitOfWork.Policies.GetByIdAsync(pid);
        if (policy == null || policy.CustomerId != customerRecord.Id)
            throw new ForbiddenException("Access denied to this policy schedule.");

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

                        // When the premium on an agent-sourced policy is paid, the agent earns a
                        // commission awaiting finance approval. Idempotent: at most one commission
                        // per premium payment, so a replayed/duplicate reconcile won't double-book.
                        if (policy != null && policy.AgentId.HasValue)
                        {
                            var existingCommission = await _unitOfWork.AgentCommissions
                                .FirstOrDefaultAsync(c => c.PremiumPaymentId == payment.Id);
                            if (existingCommission == null)
                            {
                                var agent = await _unitOfWork.Agents.GetByIdAsync(policy.AgentId.Value);
                                var rate = agent != null && agent.CommissionRate > 0 ? agent.CommissionRate : 0.05m;
                                await _unitOfWork.AgentCommissions.AddAsync(new AgentCommission
                                {
                                    Id = Guid.NewGuid(),
                                    AgentId = policy.AgentId.Value,
                                    PolicyId = policy.Id,
                                    PremiumPaymentId = payment.Id,
                                    CommissionRate = rate,
                                    CommissionAmount = Math.Round(payment.Amount * rate, 2),
                                    Status = "PENDING",
                                    CreatedAt = DateTimeOffset.UtcNow
                                });
                            }
                        }

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

                                    await _emailService.SendTemplatedEmailAsync("PolicyActivated", new Dictionary<string, string>
                                    {
                                        ["firstName"]      = WebUtility.HtmlEncode(user.FirstName),
                                        ["policyNumber"]   = WebUtility.HtmlEncode(policy.PolicyNumber),
                                        ["product"]        = WebUtility.HtmlEncode(productName),
                                        ["sumAssured"]     = $"{policy.SumAssured:0.00}",
                                        ["premiumAmount"]  = $"{policy.PremiumAmount:0.00}",
                                        ["frequency"]      = WebUtility.HtmlEncode(policy.PaymentFrequency),
                                        ["startDate"]      = $"{policy.StartDate:dd MMM yyyy}",
                                        ["endDate"]        = $"{policy.EndDate:dd MMM yyyy}",
                                        ["status"]         = WebUtility.HtmlEncode(policy.Status.ToString()),
                                    }, user.Email, attachment);
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

            // Send payment confirmation email for all successful premium payments
            if (payment.ScheduleId.HasValue)
            {
                var confirmedSchedule = await _unitOfWork.PremiumSchedules.GetByIdAsync(payment.ScheduleId.Value);
                if (confirmedSchedule?.PolicyId.HasValue == true)
                {
                    var confirmedPolicy = await _unitOfWork.Policies.GetByIdAsync(confirmedSchedule.PolicyId.Value);
                    if (confirmedPolicy != null)
                    {
                        var confirmedCustomer = await _unitOfWork.Customers.GetByIdAsync(confirmedPolicy.CustomerId);
                        if (confirmedCustomer != null)
                        {
                            var confirmedUser = await _unitOfWork.Users.GetByIdAsync(confirmedCustomer.UserId);
                            if (confirmedUser != null)
                                await _emailService.SendTemplatedEmailAsync("PremiumPaymentConfirmed", new Dictionary<string, string>
                                {
                                    ["firstName"]          = WebUtility.HtmlEncode(confirmedUser.FirstName),
                                    ["policyNumber"]       = WebUtility.HtmlEncode(confirmedPolicy.PolicyNumber),
                                    ["installmentNumber"]  = confirmedSchedule.InstallmentNumber.ToString(),
                                    ["amount"]             = $"{payment.Amount:0.00} {payment.Currency}"
                                }, confirmedUser.Email);
                        }
                    }
                }
            }

            _logger.LogInformation("Payment {PaymentId} reconciled as Paid", payment.Id);
            await _unitOfWork.CompleteAsync();
        }
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

        PremiumSchedule? overdueSchedule = null;
        if (payment.ScheduleId.HasValue)
        {
            overdueSchedule = await _unitOfWork.PremiumSchedules.GetByIdAsync(payment.ScheduleId.Value);
            if (overdueSchedule != null)
                overdueSchedule.Status = PremiumScheduleStatus.Overdue;
        }
        await _unitOfWork.CompleteAsync();

        if (overdueSchedule != null)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(payment.CustomerId);
            if (customer != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
                var overduePolicy = overdueSchedule.PolicyId.HasValue
                    ? await _unitOfWork.Policies.GetByIdAsync(overdueSchedule.PolicyId.Value)
                    : null;
                if (user != null && overduePolicy != null)
                {
                    await _emailService.SendTemplatedEmailAsync("PremiumOverdue", new Dictionary<string, string>
                    {
                        ["firstName"]    = WebUtility.HtmlEncode(user.FirstName),
                        ["policyNumber"] = WebUtility.HtmlEncode(overduePolicy.PolicyNumber),
                        ["amount"]       = $"{overdueSchedule.Amount:0.00}",
                        ["dueDate"]      = $"{overdueSchedule.DueDate:dd MMM yyyy}"
                    }, user.Email);
                }
            }
        }
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

        var claimCustomer = await _unitOfWork.Customers.GetByIdAsync(claim.CustomerId);
        if (claimCustomer != null)
        {
            var claimUser = await _unitOfWork.Users.GetByIdAsync(claimCustomer.UserId);
            if (claimUser != null)
            {
                await _emailService.SendTemplatedEmailAsync("ClaimSettled", new Dictionary<string, string>
                {
                    ["firstName"]    = WebUtility.HtmlEncode(claimUser.FirstName),
                    ["claimNumber"]  = WebUtility.HtmlEncode(claim.ClaimNumber),
                    ["payoutAmount"] = $"{claim.ClaimAmountApproved ?? 0:0.00}"
                }, claimUser.Email);
            }
        }
    }

    public async Task MarkClaimFinanciallySettledAsync(string claimId, string financeOfficerId)
    {
        if (!Guid.TryParse(claimId, out var cId)) throw new ValidationException("Invalid Claim ID");
        var claim = await _unitOfWork.Claims.GetByIdAsync(cId);
        if (claim == null) throw new NotFoundException("Claim not found.");

        if (claim.Status == ClaimStatus.Settled)
            throw new ConflictException("Claim is already marked as settled.");

        if (claim.Status != ClaimStatus.Approved)
            throw new UnprocessableException("Claim must be approved before financial settlement.");

        var oldStatus = claim.Status;
        claim.Status = ClaimStatus.Settled;
        claim.SettlementDate = DateTime.UtcNow;
        claim.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.ClaimStatusHistories.AddAsync(new ClaimStatusHistory
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            OldStatus = oldStatus,
            NewStatus = ClaimStatus.Settled,
            ChangedById = Guid.Parse(financeOfficerId),
            Notes = "Marked as financially settled by Finance Officer.",
            ChangedAt = DateTimeOffset.UtcNow
        });

        await _unitOfWork.CompleteAsync();

        var settledCustomer = await _unitOfWork.Customers.GetByIdAsync(claim.CustomerId);
        if (settledCustomer != null)
        {
            var settledUser = await _unitOfWork.Users.GetByIdAsync(settledCustomer.UserId);
            if (settledUser != null)
            {
                await _emailService.SendTemplatedEmailAsync("ClaimSettled", new Dictionary<string, string>
                {
                    ["firstName"]    = WebUtility.HtmlEncode(settledUser.FirstName),
                    ["claimNumber"]  = WebUtility.HtmlEncode(claim.ClaimNumber),
                    ["payoutAmount"] = $"{claim.ClaimAmountApproved ?? 0:0.00}"
                }, settledUser.Email);
            }
        }
    }

    public async Task<IEnumerable<AgentCommissionDto>> GetPendingCommissionsAsync()
    {
        var commissions = await _unitOfWork.AgentCommissions.FindAsync(c => c.Status == "PENDING");
        var commissionList = commissions.ToList();
        var agentIds = commissionList.Select(c => c.AgentId).Distinct().ToList();
        var policyIds = commissionList.Select(c => c.PolicyId).Distinct().ToList();
        var agents = agentIds.Count == 0
            ? new List<Agent>()
            : (await _unitOfWork.Agents.FindAsync(a => agentIds.Contains(a.Id))).ToList();
        var policies = policyIds.Count == 0
            ? new List<Policy>()
            : (await _unitOfWork.Policies.FindAsync(p => policyIds.Contains(p.Id))).ToList();
        var userIds = agents.Select(a => a.UserId).Distinct().ToList();
        var productIds = policies.Select(p => p.ProductId).Distinct().ToList();
        var users = userIds.Count == 0
            ? new List<User>()
            : (await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id))).ToList();
        var products = productIds.Count == 0
            ? new List<InsuranceProduct>()
            : (await _unitOfWork.InsuranceProducts.FindAsync(p => productIds.Contains(p.Id))).ToList();

        var agentMap = agents.ToDictionary(a => a.Id);
        var policyMap = policies.ToDictionary(p => p.Id);
        var userMap = users.ToDictionary(u => u.Id);
        var productMap = products.ToDictionary(p => p.Id);

        return commissionList.Select(c =>
        {
            agentMap.TryGetValue(c.AgentId, out var agent);
            policyMap.TryGetValue(c.PolicyId, out var policy);
            var agentUser = agent != null && userMap.TryGetValue(agent.UserId, out var user) ? user : null;
            var product = policy != null && productMap.TryGetValue(policy.ProductId, out var foundProduct) ? foundProduct : null;

            return new AgentCommissionDto
            {
                Id = c.Id,
                AgentId = c.AgentId,
                AgentName = agentUser == null ? "Unassigned agent" : $"{agentUser.FirstName} {agentUser.LastName}".Trim(),
                PolicyId = c.PolicyId,
                PolicyNumber = policy?.PolicyNumber ?? string.Empty,
                Domain = product?.Domain ?? string.Empty,
                CommissionRate = c.CommissionRate * 100,
                CommissionAmount = c.CommissionAmount,
                Status = NormalizeCommissionStatus(c.Status),
                CreatedAt = c.CreatedAt,
                PaidAt = c.PaidAt
            };
        });
    }

    public async Task ApproveAndPayCommissionAsync(string commissionId, string financeOfficerId)
    {
        if (!Guid.TryParse(commissionId, out var cid)) throw new ValidationException("Invalid Commission ID");
        var commission = await _unitOfWork.AgentCommissions.GetByIdAsync(cid);
        if (commission == null) throw new NotFoundException("Commission not found");
        if (commission.Status == "PAID")
            throw new ConflictException("Commission is already paid.");
        if (commission.Status != "PENDING")
            throw new UnprocessableException("Only pending commissions can be approved.");

        commission.Status = "PAID";
        commission.PaidAt = DateTimeOffset.UtcNow;
        await _unitOfWork.CompleteAsync();

        var commAgent = await _unitOfWork.Agents.GetByIdAsync(commission.AgentId);
        if (commAgent != null)
        {
            var commUser = await _unitOfWork.Users.GetByIdAsync(commAgent.UserId);
            if (commUser != null)
            {
                var commPolicy = commission.PolicyId != Guid.Empty
                    ? await _unitOfWork.Policies.GetByIdAsync(commission.PolicyId)
                    : null;
                await _emailService.SendTemplatedEmailAsync("CommissionCredited", new Dictionary<string, string>
                {
                    ["firstName"]        = WebUtility.HtmlEncode(commUser.FirstName),
                    ["policyNumber"]     = WebUtility.HtmlEncode(commPolicy?.PolicyNumber ?? "N/A"),
                    ["commissionAmount"] = $"{commission.CommissionAmount:0.00}"
                }, commUser.Email);
            }
        }
    }

    public async Task<IEnumerable<OverduePolicyDto>> GetOverduePoliciesAsync()
    {
        var overdueSchedules = await _unitOfWork.PremiumSchedules.FindAsync(s => s.Status == PremiumScheduleStatus.Overdue);
        var scheduleList = overdueSchedules.ToList();
        var policyIds = scheduleList.Where(s => s.PolicyId.HasValue).Select(s => s.PolicyId!.Value).Distinct().ToList();
        var policies = policyIds.Count == 0
            ? new List<Policy>()
            : (await _unitOfWork.Policies.FindAsync(p => policyIds.Contains(p.Id))).ToList();
        var customerIds = policies.Select(p => p.CustomerId).Distinct().ToList();
        var productIds = policies.Select(p => p.ProductId).Distinct().ToList();
        var customers = customerIds.Count == 0
            ? new List<Models.Customer>()
            : (await _unitOfWork.Customers.FindAsync(c => customerIds.Contains(c.Id))).ToList();
        var overdueCustomerUserIds = customers.Select(c => c.UserId).Distinct().ToList();
        var users = overdueCustomerUserIds.Count == 0
            ? new List<User>()
            : (await _unitOfWork.Users.FindAsync(u => overdueCustomerUserIds.Contains(u.Id))).ToList();
        var products = productIds.Count == 0
            ? new List<InsuranceProduct>()
            : (await _unitOfWork.InsuranceProducts.FindAsync(p => productIds.Contains(p.Id))).ToList();

        var policyMap = policies.ToDictionary(p => p.Id);
        var customerMap = customers.ToDictionary(c => c.Id);
        var userMap = users.ToDictionary(u => u.Id);
        var productMap = products.ToDictionary(p => p.Id);

        return scheduleList.Select(s =>
        {
            var policy = s.PolicyId.HasValue && policyMap.TryGetValue(s.PolicyId.Value, out var foundPolicy) ? foundPolicy : null;
            var customer = policy != null && customerMap.TryGetValue(policy.CustomerId, out var foundCustomer) ? foundCustomer : null;
            var user = customer != null && userMap.TryGetValue(customer.UserId, out var foundUser) ? foundUser : null;
            var product = policy != null && productMap.TryGetValue(policy.ProductId, out var foundProduct) ? foundProduct : null;

            return new OverduePolicyDto
            {
                PolicyId = s.PolicyId,
                PolicyNumber = policy?.PolicyNumber ?? string.Empty,
                CustomerName = user == null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim(),
                Domain = product?.Domain ?? string.Empty,
                AmountDue = s.Amount,
                DaysOverdue = Math.Max(0, (DateTime.UtcNow.Date - s.DueDate.Date).Days),
                DueDate = s.DueDate
            };
        });
    }

    private static string NormalizeCommissionStatus(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "PENDING" => "Pending",
            "PAID" => "Paid",
            _ => status
        };
    }

    public async Task<PaymentSummaryDto> GetPremiumCollectionSummaryAsync(string period)
    {
        var payments = await _unitOfWork.PremiumPayments.GetAllAsync();
        var claims = await _unitOfWork.Claims.GetAllAsync();

        // Honour the selected period when it is a "MMM yyyy" label (e.g. "Jun 2026").
        // Unrecognised values (e.g. "this_month") fall back to all-time so existing
        // callers keep working.
        DateTime? monthStart = null, monthEnd = null;
        if (DateTime.TryParseExact(period, "MMM yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            monthStart = new DateTime(parsed.Year, parsed.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            monthEnd = monthStart.Value.AddMonths(1);
        }

        var paidInPeriod = payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .Where(p => monthStart == null || (p.CreatedAt >= monthStart && p.CreatedAt < monthEnd))
            .ToList();

        var claimsPaidInPeriod = claims
            .Where(c => c.Status == ClaimStatus.Settled && c.ClaimAmountApproved.HasValue)
            .Where(c => monthStart == null ||
                        (c.SettlementDate.HasValue && c.SettlementDate >= monthStart && c.SettlementDate < monthEnd))
            .Sum(c => c.ClaimAmountApproved!.Value);

        var premiums = paidInPeriod.Sum(p => p.Amount);

        return new PaymentSummaryDto
        {
            TotalCollected = premiums,
            Premiums = premiums,
            ClaimsPaid = claimsPaidInPeriod,
            NetInflow = premiums - claimsPaidInPeriod,
            SuccessfulPayments = paidInPeriod.Count,
            FailedPayments = payments.Count(p => p.Status == PaymentStatus.Failed &&
                (monthStart == null || (p.CreatedAt >= monthStart && p.CreatedAt < monthEnd)))
        };
    }

    public async Task<byte[]> ExportPaymentReportsAsync(DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        var payments = await _unitOfWork.PremiumPayments.GetAllAsync();

        // Filter by the requested CreatedAt date range (inclusive) when provided,
        // so the exported file matches the date range the officer selected in the UI.
        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            payments = payments.Where(p => p.CreatedAt >= fromUtc);
        }
        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            payments = payments.Where(p => p.CreatedAt <= toUtc);
        }

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
