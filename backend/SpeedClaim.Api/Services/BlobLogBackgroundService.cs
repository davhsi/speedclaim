using Microsoft.Extensions.Hosting;

namespace SpeedClaim.Api.Services;

public sealed class BlobLogBackgroundService(BlobLogBatchingSink sink) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => sink.RunAsync(stoppingToken);
}
