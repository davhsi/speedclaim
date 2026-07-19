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
    private readonly ISpeedyWorkspaceClient? _workspaceClient;

    public SpeedyAssistantService(IUnitOfWork unitOfWork, ISpeedyAssistantClient client, ISpeedyWorkspaceClient? workspaceClient = null)
    {
        _unitOfWork = unitOfWork;
        _client = client;
        _workspaceClient = workspaceClient;
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
        KycRecord? kyc = null;
        var policies = new List<Policy>();
        var proposals = new List<Proposal>();
        if (customerUserId.HasValue)
        {
            customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId.Value)
                ?? throw new ForbiddenException("Customer profile is not available.");
            user = await _unitOfWork.Users.GetByIdAsync(customerUserId.Value)
                ?? throw new ForbiddenException("Customer profile is not available.");
            kyc = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == customerUserId.Value);
            policies = (await _unitOfWork.Policies.FindAsync(p => p.CustomerId == customer.Id)).ToList();
            proposals = (await _unitOfWork.Proposals.FindAsync(p => p.CustomerId == customer.Id))
                .OrderByDescending(p => p.CreatedAt).Take(10).ToList();
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
        List<Grievance> grievances = customer is null
            ? []
            : (await _unitOfWork.Grievances.FindAsync(g => g.CustomerId == customer.Id))
                .OrderByDescending(g => g.CreatedAt).Take(5).ToList();
        var policyNumbers = policies.ToDictionary(p => p.Id, p => p.PolicyNumber);

        var request = new SpeedyAssistantRequest(
            Guid.NewGuid(),
            question.Trim(),
            new SpeedyAccountSnapshot(
                user?.FirstName ?? "Guest",
                customerUserId.HasValue,
                proposals.Select(p => new SpeedyProposalSnapshot(
                    p.ProposalNumber, p.Product?.ProductName ?? "Insurance proposal", p.Status.ToString(), p.SubmittedAt ?? p.CreatedAt)).ToList(),
                policies.Select(p => new SpeedyPolicySnapshot(
                    p.PolicyNumber, p.Product?.ProductName ?? "Insurance policy", p.Status.ToString(), p.SumAssured,
                    p.PremiumAmount, p.PaymentFrequency, p.EndDate)).ToList(),
                schedules.Select(s => new SpeedyPremiumSnapshot(
                    s.PolicyId.HasValue && policyNumbers.TryGetValue(s.PolicyId.Value, out var number) ? number : "Policy",
                    s.Amount, s.DueDate, s.Status.ToString())).ToList(),
                claims.Select(c => new SpeedyClaimSnapshot(
                    c.ClaimNumber,
                    policyNumbers.TryGetValue(c.PolicyId, out var number) ? number : "Policy",
                    c.Status.ToString(), c.IntimationDate)).ToList(),
                grievances.Select(g => new SpeedyGrievanceSnapshot(
                    g.GrievanceNumber, g.Category.ToString(), g.Status.ToString(), g.CreatedAt, g.ResolvedAt)).ToList(),
                ToKycSnapshot(kyc)),
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

    public Task<SpeedyWorkspaceResponse> AnswerWorkspaceAsync(Guid? customerUserId, string question, CancellationToken cancellationToken = default) =>
        AnswerWorkspaceAsync(customerUserId, null, question, cancellationToken);

    public async Task<SpeedyWorkspaceResponse> AnswerWorkspaceAsync(Guid? customerUserId, Guid? conversationId, string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Trim().Length > 2_000)
            throw new ValidationException("Ask Speedy a question of up to 2,000 characters.");
        if (_workspaceClient is null)
            throw new InvalidOperationException("The Speedy workspace client is not configured.");

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
        KycRecord? kyc = null;
        var policies = new List<Policy>();
        var proposals = new List<Proposal>();
        if (customerUserId.HasValue)
        {
            customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == customerUserId.Value)
                ?? throw new ForbiddenException("Customer profile is not available.");
            user = await _unitOfWork.Users.GetByIdAsync(customerUserId.Value)
                ?? throw new ForbiddenException("Customer profile is not available.");
            kyc = await _unitOfWork.KycRecords.FirstOrDefaultAsync(k => k.UserId == customerUserId.Value);
            policies = (await _unitOfWork.Policies.FindAsync(p => p.CustomerId == customer.Id)).ToList();
            proposals = (await _unitOfWork.Proposals.FindAsync(p => p.CustomerId == customer.Id))
                .OrderByDescending(p => p.CreatedAt).Take(10).ToList();
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
        List<Grievance> grievances = customer is null
            ? []
            : (await _unitOfWork.Grievances.FindAsync(g => g.CustomerId == customer.Id))
                .OrderByDescending(g => g.CreatedAt).Take(5).ToList();
        var policyNumbers = policies.ToDictionary(p => p.Id, p => p.PolicyNumber);

        var request = new SpeedyWorkspaceRequest(
            Guid.NewGuid(),
            question.Trim(),
            new SpeedyAccountSnapshot(
                user?.FirstName ?? "Guest",
                customerUserId.HasValue,
                proposals.Select(p => new SpeedyProposalSnapshot(
                    p.ProposalNumber, p.Product?.ProductName ?? "Insurance proposal", p.Status.ToString(), p.SubmittedAt ?? p.CreatedAt)).ToList(),
                policies.Select(p => new SpeedyPolicySnapshot(
                    p.PolicyNumber, p.Product?.ProductName ?? "Insurance policy", p.Status.ToString(), p.SumAssured,
                    p.PremiumAmount, p.PaymentFrequency, p.EndDate)).ToList(),
                schedules.Select(s => new SpeedyPremiumSnapshot(
                    s.PolicyId.HasValue && policyNumbers.TryGetValue(s.PolicyId.Value, out var number) ? number : "Policy",
                    s.Amount, s.DueDate, s.Status.ToString())).ToList(),
                claims.Select(c => new SpeedyClaimSnapshot(
                    c.ClaimNumber,
                    policyNumbers.TryGetValue(c.PolicyId, out var number) ? number : "Policy",
                    c.Status.ToString(), c.IntimationDate)).ToList(),
                grievances.Select(g => new SpeedyGrievanceSnapshot(
                    g.GrievanceNumber, g.Category.ToString(), g.Status.ToString(), g.CreatedAt, g.ResolvedAt)).ToList(),
                ToKycSnapshot(kyc)),
            new SpeedyCatalogSnapshot(catalog));

        var response = await _workspaceClient.AnswerAsync(request, cancellationToken);
        if (customerUserId.HasValue && customer is not null)
        {
            var conversation = await GetOrCreateWorkspaceConversationAsync(customerUserId.Value, conversationId, question);
            await _unitOfWork.SpeedyWorkspaceMessages.AddRangeAsync([
                new SpeedyWorkspaceMessage
                {
                    Id = Guid.NewGuid(), ConversationId = conversation.Id, Role = SpeedyWorkspaceMessageRole.User,
                    Content = question.Trim(), CreatedAt = DateTimeOffset.UtcNow
                },
                new SpeedyWorkspaceMessage
                {
                    Id = Guid.NewGuid(), ConversationId = conversation.Id, Role = SpeedyWorkspaceMessageRole.Assistant,
                    Content = response.Answer, Intent = response.Intent, Risk = response.Risk,
                    ActionsJson = JsonSerializer.Serialize(response.Actions), Model = response.Model, CreatedAt = DateTimeOffset.UtcNow
                }
            ]);
            conversation.UpdatedAt = DateTimeOffset.UtcNow;
            conversation.RetainUntil = conversation.UpdatedAt.AddDays(30);
            // The repository query returns tracked existing conversations and AddAsync
            // tracks a new one as Added. Calling Update here changes a new conversation
            // to Modified, preventing its INSERT and breaking the message foreign key.
            await _unitOfWork.AuditLogs.AddAsync(new AuditLog
            {
                Id = Guid.NewGuid(), UserId = customerUserId.Value, EntityType = "Customer", EntityId = customer.Id,
                Action = "SpeedyWorkspaceAnswered", NewValue = JsonSerializer.Serialize(new { response.RequestId, response.Intent, response.Risk }), CreatedAt = DateTime.UtcNow
            });
            await _unitOfWork.CompleteAsync();
            response = response with { ConversationId = conversation.Id };
        }
        return response;
    }

    public async Task<IReadOnlyList<SpeedyWorkspaceConversationDto>> ListWorkspaceConversationsAsync(Guid customerUserId)
    {
        var conversations = await _unitOfWork.SpeedyWorkspaceConversations.FindAsync(c =>
            c.CreatedByUserId == customerUserId && c.RetainUntil > DateTimeOffset.UtcNow);
        return conversations.OrderByDescending(c => c.UpdatedAt).Take(30).Select(c => ToWorkspaceConversationDto(c)).ToList();
    }

    public async Task<SpeedyWorkspaceConversationDto> GetWorkspaceConversationAsync(Guid customerUserId, Guid conversationId)
    {
        var conversation = await _unitOfWork.SpeedyWorkspaceConversations.FirstOrDefaultAsync(c =>
            c.Id == conversationId && c.CreatedByUserId == customerUserId && c.RetainUntil > DateTimeOffset.UtcNow)
            ?? throw new NotFoundException("Speedy conversation not found.");
        var messages = await _unitOfWork.SpeedyWorkspaceMessages.FindAsync(m => m.ConversationId == conversationId);
        return ToWorkspaceConversationDto(conversation, messages.OrderBy(m => m.CreatedAt).Select(ToWorkspaceMessageDto).ToList());
    }

    private async Task<SpeedyWorkspaceConversation> GetOrCreateWorkspaceConversationAsync(Guid customerUserId, Guid? conversationId, string question)
    {
        if (conversationId.HasValue)
            return await _unitOfWork.SpeedyWorkspaceConversations.FirstOrDefaultAsync(c =>
                c.Id == conversationId.Value && c.CreatedByUserId == customerUserId && c.RetainUntil > DateTimeOffset.UtcNow)
                ?? throw new NotFoundException("Speedy conversation not found.");

        var now = DateTimeOffset.UtcNow;
        var conversation = new SpeedyWorkspaceConversation
        {
            Id = Guid.NewGuid(), CreatedByUserId = customerUserId,
            Title = question.Trim()[..Math.Min(question.Trim().Length, 120)], CreatedAt = now, UpdatedAt = now, RetainUntil = now.AddDays(30)
        };
        await _unitOfWork.SpeedyWorkspaceConversations.AddAsync(conversation);
        return conversation;
    }

    private static SpeedyWorkspaceConversationDto ToWorkspaceConversationDto(SpeedyWorkspaceConversation conversation, IReadOnlyList<SpeedyWorkspaceMessageDto>? messages = null) =>
        new(conversation.Id, conversation.Title, conversation.CreatedAt, conversation.UpdatedAt, messages);

    private static SpeedyKycSnapshot? ToKycSnapshot(KycRecord? kyc) => kyc is null
        ? null
        : new SpeedyKycSnapshot(
            kyc.KycStatus.ToString(),
            !string.IsNullOrWhiteSpace(kyc.AadhaarDocumentKey),
            !string.IsNullOrWhiteSpace(kyc.PanDocumentKey));

    private static SpeedyWorkspaceMessageDto ToWorkspaceMessageDto(SpeedyWorkspaceMessage message)
    {
        var actions = string.IsNullOrWhiteSpace(message.ActionsJson)
            ? []
            : JsonSerializer.Deserialize<List<SpeedyWorkspaceAction>>(message.ActionsJson) ?? [];
        return new(message.Id, message.Role.ToString(), message.Content, message.Intent, message.Risk, actions, message.CreatedAt);
    }
}
