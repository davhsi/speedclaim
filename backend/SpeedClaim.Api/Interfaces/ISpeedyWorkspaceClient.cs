using SpeedClaim.Api.Dtos.Assistant;

namespace SpeedClaim.Api.Interfaces;

public interface ISpeedyWorkspaceClient
{
    Task<SpeedyWorkspaceResponse> AnswerAsync(SpeedyWorkspaceRequest request, CancellationToken cancellationToken = default);
}
