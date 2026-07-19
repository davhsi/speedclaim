using SpeedClaim.Api.Dtos.Assistant;

namespace SpeedClaim.Api.Interfaces;

public interface ISpeedyAssistantService
{
    Task<SpeedyAssistantResponse> AnswerAsync(Guid? customerUserId, string question, CancellationToken cancellationToken = default);
}
