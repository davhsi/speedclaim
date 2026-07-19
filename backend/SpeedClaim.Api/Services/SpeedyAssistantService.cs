using System.Text.Json;
using SpeedClaim.Api.Dtos.Assistant;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public sealed class SpeedyAssistantService : ISpeedyAssistantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpeedyAssistantClient _client;

    public SpeedyAssistantService(IUnitOfWork unitOfWork, ISpeedyAssistantClient client)
    {
        _unitOfWork = unitOfWork;
        _client = client;
    }

    public async Task<SpeedyAssistantResponse> AnswerAsync(Guid? customerUserId, string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Trim().Length > 2_000)
            throw new ValidationException("Ask Speedy a question of up to 2,000 characters.");

        var catalog = (await _unitOfWork.InsuranceProducts.FindAsync(p => p.IsActive && p.IsAvailableForSale))
            .OrderBy(p => p.ProductName)
            .Take(50)
            .Select(p => new SpeedyProductSnapshot(
                p.ProductName, p.Domain, p.Description, p.MinAge, p.MaxAge, p.MinSumAssured, p.MaxSumAssured,
                p.MinTenureYears, p.MaxTenureYears, p.WaitingPeriodDays, p.AllowsFamilyFloater, p.MaxFamilyMembers,
                p.MotorVehicleType))
            .ToList();

        Customer? customer = null;
        User? user = null;
        var policies = new List<Policy>();
        if (customerUserId.HasValue)
        {
            customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId.Value)
                ?? throw new ForbiddenException("Customer profile is not available.");
            user = await _unitOfWork.Users.GetByIdAsync(customerUserId.Value)
                ?? throw new ForbiddenException("Customer profile is not available.");
            policies = (await _unitOfWork.Policies.FindAsync(p => p.CustomerId == customer.Id)).ToList();
        }

        var policyIds = policies.Select(p => p.Id).ToHashSet();
        var schedules = policyIds.Count == 0
            ? []
            : (await _unitOfWork.PremiumSchedules.FindAsync(s => s.PolicyId.HasValue && policyIds.Contains(s.PolicyId.GetValueOrDefault())))
                .Where(s => s.Status is PremiumScheduleStatus.Upcoming or PremiumScheduleStatus.Due or PremiumScheduleStatus.Overdue)
                .OrderBy(s => s.DueDate).Take(5).ToList();
        List<Claim> claims = customer is null
            ? []
            : (await _unitOfWork.Claims.FindAsync(c => c.CustomerId == customer.Id))
                .OrderByDescending(c => c.IntimationDate).Take(5).ToList();
        var policyNumbers = policies.ToDictionary(p => p.Id, p => p.PolicyNumber);

        var request = new SpeedyAssistantRequest(
            Guid.NewGuid(),
            question.Trim(),
            new SpeedyAccountSnapshot(
                user?.FirstName ?? "Guest",
                customerUserId.HasValue,
                policies.Select(p => new SpeedyPolicySnapshot(
                    p.PolicyNumber, p.Product?.ProductName ?? "Insurance policy", p.Status.ToString(), p.SumAssured,
                    p.PremiumAmount, p.PaymentFrequency, p.EndDate)).ToList(),
                schedules.Select(s => new SpeedyPremiumSnapshot(
                    s.PolicyId.HasValue && policyNumbers.TryGetValue(s.PolicyId.Value, out var number) ? number : "Policy",
                    s.Amount, s.DueDate, s.Status.ToString())).ToList(),
                claims.Select(c => new SpeedyClaimSnapshot(
                    c.ClaimNumber,
                    policyNumbers.TryGetValue(c.PolicyId, out var number) ? number : "Policy",
                    c.Status.ToString(), c.IntimationDate)).ToList()),
            new SpeedyCatalogSnapshot(catalog));

        var response = await _client.AnswerAsync(request, cancellationToken);
        if (customerUserId.HasValue && customer is not null)
        {
            await _unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(), UserId = customerUserId.Value, EntityType = "Customer", EntityId = customer.Id,
                Action = "SpeedyAnswered", NewValue = JsonSerializer.Serialize(new { response.RequestId }), CreatedAt = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();
        }
        return response;
    }
}
