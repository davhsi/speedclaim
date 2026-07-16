using SpeedClaim.Api.Contracts;

namespace SpeedClaim.Api.Interfaces;

public interface IEmailDispatchQueue
{
    Task EnqueueAsync(EmailDispatchMessage message, CancellationToken cancellationToken = default);
}
