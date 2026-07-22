using System.Text.Json;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Mcp;

/// <summary>
/// Curated, minimal customer projections for the public MCP surface.
/// This deliberately does not reuse or expose REST controllers, document paths, or sensitive fields.
/// </summary>
public sealed class McpReadOnlyToolService
{
    private readonly IUnitOfWork _unitOfWork;
    public McpReadOnlyToolService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<object> ExecuteAsync(string toolName, Guid? userId, JsonElement arguments, string subject)
    {
        if (toolName == "get_available_products")
        {
            var products = await _unitOfWork.InsuranceProducts.FindAsync(p => p.IsActive && p.IsAvailableForSale);
            return products.OrderBy(p => p.ProductName).Select(p => new
            {
                p.ProductName, p.Domain, p.Description, p.MinAge, p.MaxAge, p.MinSumAssured, p.MaxSumAssured,
                p.MinTenureYears, p.MaxTenureYears, p.WaitingPeriodDays, p.AllowsFamilyFloater, p.MaxFamilyMembers,
                p.MotorVehicleType
            }).ToList();
        }

        if (toolName == "select_published_brochure")
        {
            var requestedProduct = arguments.TryGetProperty("productName", out var productName) ? productName.GetString() : null;
            var products = (await _unitOfWork.InsuranceProducts.FindAsync(p => p.IsActive && p.IsAvailableForSale))
                .ToDictionary(p => p.Id, p => p.ProductName);
            var brochures = await _unitOfWork.ProductBrochures.FindAsync(b => b.Status == ProductBrochureStatus.Published);
            return brochures
                .Where(b => products.ContainsKey(b.ProductId) && (string.IsNullOrWhiteSpace(requestedProduct) || string.Equals(products[b.ProductId], requestedProduct, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(b => b.PublishedAt)
                .Select(b => new { productName = products[b.ProductId], b.Version, b.EffectiveFrom, b.EffectiveTo, b.PageCount, b.PublishedAt })
                .ToList();
        }

        if (userId is null)
            throw new ForbiddenException("Link your SpeedClaim account before using account tools.");

        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId.Value)
            ?? throw new ForbiddenException("A SpeedClaim customer profile was not found for this identity.");

        if (toolName == "get_my_kyc_next_step")
        {
            var kyc = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == userId.Value);
            return kyc is null
                ? new { status = "NotStarted", nextStep = "Submit Aadhaar and PAN in the secure SpeedClaim portal." }
                : new { status = kyc.KycStatus.ToString(), hasAadhaarDocument = !string.IsNullOrWhiteSpace(kyc.AadhaarDocumentKey), hasPanDocument = !string.IsNullOrWhiteSpace(kyc.PanDocumentKey), nextStep = KycNextStep(kyc.KycStatus) };
        }

        var productsById = (await _unitOfWork.InsuranceProducts.GetAllAsync()).ToDictionary(p => p.Id, p => p.ProductName);
        if (toolName == "get_my_policy_summary")
        {
            var policies = await _unitOfWork.Policies.FindAsync(p => p.CustomerId == customer.Id);
            return policies.OrderByDescending(p => p.EndDate).Select(p => new { p.PolicyNumber, productName = ProductName(productsById, p.ProductId), status = p.Status.ToString(), p.SumAssured, p.PremiumAmount, p.PaymentFrequency, p.StartDate, p.EndDate }).ToList();
        }
        if (toolName == "get_my_proposal_status")
        {
            var proposals = await _unitOfWork.Proposals.FindAsync(p => p.CustomerId == customer.Id);
            return proposals.OrderByDescending(p => p.CreatedAt).Take(10).Select(p => new { p.ProposalNumber, productName = ProductName(productsById, p.ProductId), status = p.Status.ToString(), p.SubmittedAt, p.CreatedAt }).ToList();
        }
        if (toolName == "get_my_next_premium_due")
        {
            var policies = await _unitOfWork.Policies.FindAsync(p => p.CustomerId == customer.Id);
            var numbers = policies.ToDictionary(p => p.Id, p => p.PolicyNumber);
            var ids = numbers.Keys.ToHashSet();
            var schedules = await _unitOfWork.PremiumSchedules.FindAsync(s => s.PolicyId.HasValue && ids.Contains(s.PolicyId.Value) && (s.Status == PremiumScheduleStatus.Upcoming || s.Status == PremiumScheduleStatus.Due || s.Status == PremiumScheduleStatus.Overdue));
            return schedules.OrderBy(s => s.DueDate).Take(5).Select(s => new { policyNumber = numbers[s.PolicyId!.Value], s.Amount, s.DueDate, status = s.Status.ToString() }).ToList();
        }
        if (toolName == "get_my_claim_status")
        {
            var claims = await _unitOfWork.Claims.FindAsync(c => c.CustomerId == customer.Id);
            return claims.OrderByDescending(c => c.IntimationDate).Take(10).Select(c => new { c.ClaimNumber, status = c.Status.ToString(), c.IntimationDate, c.IncidentDate, claimType = c.ClaimType.ToString() }).ToList();
        }
        if (toolName == "get_my_grievance_status")
        {
            var grievances = await _unitOfWork.Grievances.FindAsync(g => g.CustomerId == customer.Id);
            return grievances.OrderByDescending(g => g.CreatedAt).Take(10).Select(g => new { g.GrievanceNumber, category = g.Category.ToString(), status = g.Status.ToString(), g.CreatedAt, g.ResolvedAt }).ToList();
        }
        if (toolName == "get_customer_assistance")
            return new { message = "SpeedClaim MCP is read-only. Use the SpeedClaim web app for applications, payments, claims, KYC uploads, and any action requiring confirmation." };

        throw new ValidationException("Unknown or unavailable MCP tool.");
    }

    public async Task AuditInvocationAsync(Guid? userId, string toolName, string subject, string? clientId, string scope, string outcome)
    {
        await _unitOfWork.AuditLogs.AddAsync(new SpeedClaim.Api.Models.AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntityType = "McpTool",
            EntityId = Guid.NewGuid(),
            Action = "McpExternalToolInvoked",
            NewValue = JsonSerializer.Serialize(new { toolName, subject, clientId, scope, outcome, surface = "external" }),
            CreatedAt = DateTime.UtcNow
        });
        _unitOfWork.SetCurrentActor(userId);
        await _unitOfWork.CompleteAsync();
    }

    private static string ProductName(IReadOnlyDictionary<Guid, string> productsById, Guid id) =>
        productsById.TryGetValue(id, out var productName) ? productName : "Insurance product";

    private static string KycNextStep(KycStatus status) => status switch
    {
        KycStatus.Approved => "No further KYC action is needed.",
        KycStatus.UnderReview => "Your KYC is under review. Do not resubmit documents unless asked.",
        KycStatus.Rejected => "Review the rejection message in the secure SpeedClaim portal and resubmit the requested documents.",
        _ => "Submit both Aadhaar and PAN in the secure SpeedClaim portal."
    };
}
