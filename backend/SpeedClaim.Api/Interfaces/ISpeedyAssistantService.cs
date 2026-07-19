using SpeedClaim.Api.Dtos.Assistant;

namespace SpeedClaim.Api.Interfaces;

public interface ISpeedyAssistantService
{
    Task<SpeedyAssistantResponse> AnswerAsync(Guid? customerUserId, string question, CancellationToken cancellationToken = default);
    Task<SpeedyWorkspaceResponse> AnswerWorkspaceAsync(Guid? customerUserId, string question, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpeedyWorkspaceConversationDto>> ListWorkspaceConversationsAsync(Guid customerUserId);
    Task<SpeedyWorkspaceConversationDto> GetWorkspaceConversationAsync(Guid customerUserId, Guid conversationId);
    Task<SpeedyWorkspaceResponse> AnswerWorkspaceAsync(Guid? customerUserId, Guid? conversationId, string question, CancellationToken cancellationToken = default);
}
