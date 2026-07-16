using System.Text.Json;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public sealed class PolicyAssistantService : IPolicyAssistantService
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(365);
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPolicyQaClient _client;
    private readonly ILogger<PolicyAssistantService> _logger;

    public PolicyAssistantService(IUnitOfWork unitOfWork, IPolicyQaClient client, ILogger<PolicyAssistantService> logger)
    {
        _unitOfWork = unitOfWork;
        _client = client;
        _logger = logger;
    }

    public async Task<PolicyAssistantAvailabilityDto> GetAvailabilityAsync(Guid policyId, Guid actorId, bool isCustomer)
    {
        var policy = await AuthorizePolicyAsync(policyId, actorId, isCustomer);
        if (!policy.ProductBrochureId.HasValue)
            return new(false, "LegacyPolicy", null, null);
        var brochure = await _unitOfWork.ProductBrochures.GetByIdAsync(policy.ProductBrochureId.Value);
        if (brochure is null || brochure.Status is ProductBrochureStatus.Failed or ProductBrochureStatus.Processing)
            return new(false, "Unavailable", brochure?.Version, brochure?.EffectiveFrom);
        if (brochure.Status is ProductBrochureStatus.Archived or ProductBrochureStatus.Published)
            return new(true, "Ready", brochure.Version, brochure.EffectiveFrom);
        return new(false, brochure.Status.ToString(), brochure.Version, brochure.EffectiveFrom);
    }

    public async Task<IReadOnlyList<PolicyAssistantConversationDto>> ListAsync(Guid policyId, Guid actorId, bool isCustomer)
    {
        await AuthorizePolicyAsync(policyId, actorId, isCustomer);
        await RemoveExpiredAsync();
        var conversations = await _unitOfWork.PolicyAssistantConversations.FindAsync(c =>
            c.PolicyId == policyId && c.CreatedByUserId == actorId && c.RetainUntil > DateTimeOffset.UtcNow);
        return conversations.OrderByDescending(c => c.UpdatedAt).Select(c => ToDto(c, null)).ToList();
    }

    public async Task<PolicyAssistantConversationDto> CreateAsync(Guid policyId, Guid actorId, bool isCustomer)
    {
        var policy = await AuthorizePolicyAsync(policyId, actorId, isCustomer);
        var brochure = await RequireBoundBrochureAsync(policy);
        var now = DateTimeOffset.UtcNow;
        var conversation = new PolicyAssistantConversation
        {
            Id = Guid.NewGuid(), PolicyId = policy.Id, BrochureId = brochure.Id, CreatedByUserId = actorId,
            CreatedAt = now, UpdatedAt = now, RetainUntil = now.Add(Retention)
        };
        await _unitOfWork.PolicyAssistantConversations.AddAsync(conversation);
        await _unitOfWork.CompleteAsync();
        await AuditAsync(actorId, policy.Id, "PolicyGuideConversationCreated", new { brochureId = brochure.Id });
        return ToDto(conversation, brochure.Version);
    }

    public async Task<PolicyAssistantConversationDto> GetAsync(Guid policyId, Guid conversationId, Guid actorId, bool isCustomer)
    {
        await AuthorizePolicyAsync(policyId, actorId, isCustomer);
        await RemoveExpiredAsync();
        var conversation = await GetOwnedConversationAsync(policyId, conversationId, actorId);
        var brochure = await _unitOfWork.ProductBrochures.GetByIdAsync(conversation.BrochureId)
            ?? throw new NotFoundException("Brochure not found.");
        var messages = await _unitOfWork.PolicyAssistantMessages.FindAsync(m => m.ConversationId == conversationId);
        return ToDto(conversation, brochure.Version, messages.OrderBy(m => m.CreatedAt).Select(ToMessage).ToList());
    }

    public async Task<PolicyAssistantAnswerDto> SendAsync(Guid policyId, Guid conversationId, string question, Guid actorId, bool isCustomer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Length > 2000)
            throw new ValidationException("Enter a policy question of up to 2,000 characters.");
        var policy = await AuthorizePolicyAsync(policyId, actorId, isCustomer);
        var conversation = await GetOwnedConversationAsync(policyId, conversationId, actorId);
        if (policy.ProductBrochureId != conversation.BrochureId)
            throw new ConflictException("This conversation is bound to a different brochure version.");
        var brochure = await RequireBoundBrochureAsync(policy);
        var requestId = Guid.NewGuid();
        var userMessage = new PolicyAssistantMessage { Id = Guid.NewGuid(), ConversationId = conversation.Id, Role = PolicyAssistantMessageRole.User, Content = question.Trim() };
        await _unitOfWork.PolicyAssistantMessages.AddAsync(userMessage);
        await _unitOfWork.CompleteAsync();

        PolicyQaResponse answer;
        try
        {
            answer = await _client.AnswerAsync(new(requestId, brochure.Id, policy.ProductId, brochure.Version, userMessage.Content), cancellationToken);
        }
        catch
        {
            await AuditAsync(actorId, policy.Id, "PolicyGuideAnswerFailed", new { brochureId = brochure.Id, requestId });
            throw;
        }
        if (answer.EvidenceStatus is not ("Grounded" or "InsufficientEvidence" or "Rejected"))
            throw new BrochureIngestionException("invalid_policy_qa_response", "Policy Guide returned an invalid evidence status.");

        var assistantMessage = new PolicyAssistantMessage
        {
            Id = Guid.NewGuid(), ConversationId = conversation.Id, Role = PolicyAssistantMessageRole.Assistant,
            Content = answer.Answer, EvidenceStatus = answer.EvidenceStatus,
            CitationsJson = JsonSerializer.Serialize(answer.Citations), Model = answer.Model, PromptVersion = answer.PromptVersion
        };
        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        conversation.RetainUntil = conversation.UpdatedAt.Add(Retention);
        _unitOfWork.PolicyAssistantConversations.Update(conversation);
        await _unitOfWork.PolicyAssistantMessages.AddAsync(assistantMessage);
        await _unitOfWork.CompleteAsync();
        await AuditAsync(actorId, policy.Id, "PolicyGuideAnswered", new { brochureId = brochure.Id, requestId, evidenceStatus = answer.EvidenceStatus, model = answer.Model, promptVersion = answer.PromptVersion });
        _logger.LogInformation("Policy Guide answered policy {PolicyId}; request {RequestId}; evidence {EvidenceStatus}", policy.Id, requestId, answer.EvidenceStatus);
        return new(requestId, conversation.Id, assistantMessage.Id, answer.Answer, answer.EvidenceStatus, brochure.Version, answer.Citations);
    }

    private async Task<Policy> AuthorizePolicyAsync(Guid policyId, Guid actorId, bool isCustomer)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId) ?? throw new NotFoundException("Policy not found.");
        if (isCustomer)
        {
            var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == actorId);
            if (customer is null || customer.Id != policy.CustomerId)
                throw new ForbiddenException("Access denied to this policy.");
        }
        return policy;
    }

    private async Task<ProductBrochure> RequireBoundBrochureAsync(Policy policy)
    {
        if (!policy.ProductBrochureId.HasValue)
            throw new ConflictException("This legacy policy has no brochure version bound to it.");
        var brochure = await _unitOfWork.ProductBrochures.GetByIdAsync(policy.ProductBrochureId.Value)
            ?? throw new ConflictException("The brochure bound to this policy is unavailable.");
        if (brochure.Status is not (ProductBrochureStatus.Published or ProductBrochureStatus.Archived) || brochure.PageCount <= 0 || brochure.ChildChunkCount <= 0)
            throw new ConflictException("The brochure bound to this policy is not ready for Policy Guide.");
        return brochure;
    }

    private async Task<PolicyAssistantConversation> GetOwnedConversationAsync(Guid policyId, Guid conversationId, Guid actorId) =>
        await _unitOfWork.PolicyAssistantConversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.PolicyId == policyId && c.CreatedByUserId == actorId && c.RetainUntil > DateTimeOffset.UtcNow)
        ?? throw new NotFoundException("Policy Guide conversation not found.");

    private async Task RemoveExpiredAsync()
    {
        var expired = await _unitOfWork.PolicyAssistantConversations.FindAsync(c => c.RetainUntil <= DateTimeOffset.UtcNow);
        foreach (var conversation in expired) _unitOfWork.PolicyAssistantConversations.Delete(conversation);
        if (expired.Any()) await _unitOfWork.CompleteAsync();
    }

    private async Task AuditAsync(Guid actorId, Guid policyId, string action, object metadata)
    {
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog { Id = Guid.NewGuid(), UserId = actorId, EntityType = "Policy", EntityId = policyId, Action = action, NewValue = JsonSerializer.Serialize(metadata), CreatedAt = DateTime.UtcNow });
        await _unitOfWork.CompleteAsync();
    }

    private static PolicyAssistantConversationDto ToDto(PolicyAssistantConversation conversation, string? version, IReadOnlyList<PolicyAssistantMessageDto>? messages = null) =>
        new(conversation.Id, conversation.PolicyId, conversation.BrochureId, version ?? string.Empty, conversation.CreatedAt, conversation.UpdatedAt, messages);

    private static PolicyAssistantMessageDto ToMessage(PolicyAssistantMessage message)
    {
        IReadOnlyList<PolicyAssistantCitationDto> citations = string.IsNullOrWhiteSpace(message.CitationsJson)
            ? Array.Empty<PolicyAssistantCitationDto>()
            : JsonSerializer.Deserialize<List<PolicyAssistantCitationDto>>(message.CitationsJson) ?? [];
        return new(message.Id, message.Role.ToString(), message.Content, message.EvidenceStatus, citations, message.CreatedAt);
    }
}
