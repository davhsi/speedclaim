using SpeedClaim.Api.Dtos.Assistant;

namespace SpeedClaim.Api.Interfaces;

public interface ISpeedyAssistantClient
{
    Task<SpeedyAssistantResponse> AnswerAsync(SpeedyAssistantRequest request, CancellationToken cancellationToken = default);
}
