using SpeedClaim.Api.Dtos.Policies;

namespace SpeedClaim.Api.Interfaces;

public interface IPolicyAssistantService
{
    Task<PolicyAssistantAvailabilityDto> GetAvailabilityAsync(Guid policyId, Guid actorId, bool isCustomer);
    Task<IReadOnlyList<PolicyAssistantConversationDto>> ListAsync(Guid policyId, Guid actorId, bool isCustomer);
    Task<PolicyAssistantConversationDto> CreateAsync(Guid policyId, Guid actorId, bool isCustomer);
    Task<PolicyAssistantConversationDto> GetAsync(Guid policyId, Guid conversationId, Guid actorId, bool isCustomer);
    Task<PolicyAssistantAnswerDto> SendAsync(Guid policyId, Guid conversationId, string question, Guid actorId, bool isCustomer, CancellationToken cancellationToken = default);
}
