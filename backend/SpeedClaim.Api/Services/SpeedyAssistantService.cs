using System.Text.Json;
using SpeedClaim.Api.Dtos.Assistant;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Dtos.Policies;

namespace SpeedClaim.Api.Services;

public sealed class SpeedyAssistantService : ISpeedyAssistantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpeedyAssistantClient _client;
    private readonly ISpeedyWorkspaceClient? _workspaceClient;
    private readonly IPolicyQaClient? _policyQaClient;

    public SpeedyAssistantService(IUnitOfWork unitOfWork, ISpeedyAssistantClient client, ISpeedyWorkspaceClient? workspaceClient = null, IPolicyQaClient? policyQaClient = null)
    {
        _unitOfWork = unitOfWork;
        _client = client;
        _workspaceClient = workspaceClient;
        _policyQaClient = policyQaClient;
    }

    public async Task<SpeedyAssistantResponse> AnswerAsync(Guid? customerUserId, string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Trim().Length > 2_000)
            throw new ValidationException("Ask Speedy a question of up to 2,000 characters.");

        var activeProducts = (await _unitOfWork.InsuranceProducts.FindAsync(p => p.IsActive && p.IsAvailableForSale)).ToList();
        var catalog = activeProducts
            .OrderBy(p => p.ProductName)
            .Take(50)
            .Select(p => new SpeedyProductSnapshot(
                p.ProductName, p.Domain, p.Description, p.MinAge, p.MaxAge, p.MinSumAssured, p.MaxSumAssured,
                p.MinTenureYears, p.MaxTenureYears, p.WaitingPeriodDays, p.AllowsFamilyFloater, p.MaxFamilyMembers,
                p.MotorVehicleType))
            .ToList();
        var productIdsByName = activeProducts
            .ToDictionary(p => p.Id, p => p.ProductName);
        var brochures = ((await _unitOfWork.ProductBrochures.FindAsync(b => b.Status == ProductBrochureStatus.Published)) ?? [])
            .Where(b => productIdsByName.ContainsKey(b.ProductId) && b.ChildChunkCount > 0)
            .OrderBy(b => productIdsByName[b.ProductId]).ThenByDescending(b => b.PublishedAt)
            .Select(b => new SpeedyBrochureSnapshot(b.Id, b.ProductId, productIdsByName[b.ProductId], b.Version))
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
            : (await _unitOfWork.PremiumSchedules.FindAsync(s => s.PolicyId.HasValue && policyIds.Contains(s.PolicyId.Value)))
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
                    p.Id, p.PolicyNumber, p.Product?.ProductName ?? "Insurance policy", p.Status.ToString(), p.SumAssured,
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
            new SpeedyCatalogSnapshot(catalog, brochures));

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

        var activeProducts = (await _unitOfWork.InsuranceProducts.FindAsync(p => p.IsActive && p.IsAvailableForSale)).ToList();
        var catalog = activeProducts
            .OrderBy(p => p.ProductName)
            .Take(50)
            .Select(p => new SpeedyProductSnapshot(
                p.ProductName, p.Domain, p.Description, p.MinAge, p.MaxAge, p.MinSumAssured, p.MaxSumAssured,
                p.MinTenureYears, p.MaxTenureYears, p.WaitingPeriodDays, p.AllowsFamilyFloater, p.MaxFamilyMembers,
                p.MotorVehicleType))
            .ToList();
        var productIdsByName = activeProducts
            .ToDictionary(p => p.Id, p => p.ProductName);
        var brochures = ((await _unitOfWork.ProductBrochures.FindAsync(b => b.Status == ProductBrochureStatus.Published)) ?? [])
            .Where(b => productIdsByName.ContainsKey(b.ProductId) && b.ChildChunkCount > 0)
            .OrderBy(b => productIdsByName[b.ProductId]).ThenByDescending(b => b.PublishedAt)
            .Select(b => new SpeedyBrochureSnapshot(b.Id, b.ProductId, productIdsByName[b.ProductId], b.Version))
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
            : (await _unitOfWork.PremiumSchedules.FindAsync(s => s.PolicyId.HasValue && policyIds.Contains(s.PolicyId.Value)))
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

        var policyAnswer = await TryAnswerPolicyQuestionAsync(customerUserId, policies, question, cancellationToken);
        var request = new SpeedyWorkspaceRequest(
            Guid.NewGuid(),
            question.Trim(),
            new SpeedyAccountSnapshot(
                user?.FirstName ?? "Guest",
                customerUserId.HasValue,
                proposals.Select(p => new SpeedyProposalSnapshot(
                    p.ProposalNumber, p.Product?.ProductName ?? "Insurance proposal", p.Status.ToString(), p.SubmittedAt ?? p.CreatedAt)).ToList(),
                policies.Select(p => new SpeedyPolicySnapshot(
                    p.Id, p.PolicyNumber, p.Product?.ProductName ?? "Insurance policy", p.Status.ToString(), p.SumAssured,
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
            new SpeedyCatalogSnapshot(catalog, brochures));

        var response = policyAnswer ?? await _workspaceClient.AnswerAsync(request, cancellationToken);
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
                    ActionsJson = JsonSerializer.Serialize(response.Actions), Model = response.Model,
                    EvidenceStatus = response.EvidenceStatus, BrochureVersion = response.BrochureVersion,
                    CitationsJson = response.Citations is { Count: > 0 } ? JsonSerializer.Serialize(response.Citations) : null,
                    CreatedAt = DateTimeOffset.UtcNow
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
                Action = "SpeedyWorkspaceAnswered", NewValue = JsonSerializer.Serialize(new
                {
                    response.RequestId,
                    response.Intent,
                    response.Risk,
                    ToolNames = response.ToolCalls?.Select(tool => tool.Name) ?? []
                }), CreatedAt = DateTime.UtcNow
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
        var citations = string.IsNullOrWhiteSpace(message.CitationsJson)
            ? []
            : JsonSerializer.Deserialize<List<PolicyAssistantCitationDto>>(message.CitationsJson) ?? [];
        return new(message.Id, message.Role.ToString(), message.Content, message.Intent, message.Risk, actions, [], [], message.CreatedAt,
            message.EvidenceStatus, message.BrochureVersion, citations);
    }

    private async Task<SpeedyWorkspaceResponse?> TryAnswerPolicyQuestionAsync(
        Guid? customerUserId, IReadOnlyList<Policy> policies, string question, CancellationToken cancellationToken)
    {
        if (!customerUserId.HasValue || _policyQaClient is null || !LooksLikePolicyTermsQuestion(question)) return null;

        var candidates = policies
            .Where(p => p.Status == PolicyStatus.Active && p.ProductBrochureId.HasValue)
            .OrderByDescending(p => p.EndDate)
            .ToList();
        var namedCandidates = candidates.Where(policy =>
            question.Contains(policy.PolicyNumber, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(policy.Product?.ProductName) && question.Contains(policy.Product.ProductName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (namedCandidates.Count == 1) candidates = namedCandidates;
        if (candidates.Count != 1)
        {
            var clarification = candidates.Count > 1
                ? "I can check the exact brochure terms, but you have more than one active policy. Please include the policy number or product name so I can use the right policy document."
                : "I can check brochure-grounded policy terms once an active policy with an available brochure is selected.";
            return new(Guid.NewGuid(), clarification, "policy_terms_clarification", "regulated", [], [], PolicyTermFollowUps(), null, null);
        }

        var policy = candidates[0];
        var brochure = await _unitOfWork.ProductBrochures.GetByIdAsync(policy.ProductBrochureId!.Value);
        if (brochure is null || brochure.Status is not (ProductBrochureStatus.Published or ProductBrochureStatus.Archived) || brochure.PageCount <= 0 || brochure.ChildChunkCount <= 0)
            return new(Guid.NewGuid(), "I found your policy, but its brochure evidence is not ready yet. Please try again later.", "policy_terms_unavailable", "regulated", [], [], PolicyTermFollowUps(), null, null);

        var requestId = Guid.NewGuid();
        var answer = await _policyQaClient.AnswerAsync(new PolicyQaRequest(requestId, brochure.Id, policy.ProductId, brochure.Version, question.Trim()), cancellationToken);
        if (answer.RequestId != requestId || answer.EvidenceStatus is not ("Grounded" or "InsufficientEvidence" or "Rejected"))
            throw new BrochureIngestionException("invalid_policy_qa_response", "Speedy returned an invalid policy-evidence response.");

        return new(requestId, answer.Answer, "policy_terms", "regulated", [], [], PolicyTermFollowUps(), answer.Provider, answer.Model,
            null, answer.EvidenceStatus, brochure.Version, answer.Citations);
    }

    private static bool LooksLikePolicyTermsQuestion(string question)
    {
        var normalized = question.ToLowerInvariant();
        string[] cues = ["my policy", "policy cover", "policy coverage", "hospital", "hospitalisation", "hospitalization", "exclusion", "waiting period", "room rent", "day care", "cashless", "pre-existing", "pre existing", "maternity", "sub-limit", "sub limit", "brochure", "policy terms", "claim document"];
        return cues.Any(cue => normalized.Contains(cue, StringComparison.Ordinal));
    }

    private static IReadOnlyList<string> PolicyTermFollowUps() =>
    ["What exclusions apply to my policy?", "What waiting periods should I know about?", "How do I make a claim under this policy?"];
}
