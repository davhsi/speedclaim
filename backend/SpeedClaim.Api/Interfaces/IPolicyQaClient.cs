using SpeedClaim.Api.Dtos.Policies;

namespace SpeedClaim.Api.Interfaces;

public interface IPolicyQaClient
{
    Task<PolicyQaResponse> AnswerAsync(PolicyQaRequest request, CancellationToken cancellationToken = default);
}
