using SpeedClaim.Api.Dtos.Catalog;

namespace SpeedClaim.Api.Interfaces;

public interface IBrochureIngestionClient
{
    Task<BrochureIngestionResponse> IngestAsync(
        BrochureIngestionRequest request,
        CancellationToken cancellationToken = default);
}
