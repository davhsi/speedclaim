using SpeedClaim.Api.Dtos.Catalog;

namespace SpeedClaim.Api.Interfaces;

public interface IProductBrochureService
{
    Task<ProductBrochureDto> UploadAsync(
        Guid productId,
        UploadProductBrochureRequest request,
        Guid adminId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductBrochureDto>> ListAsync(Guid productId);
    Task<ProductBrochureDto> GetAsync(Guid productId, Guid brochureId);
    Task<ProductBrochureDto> PublishAsync(Guid productId, Guid brochureId, Guid adminId);
    Task<ProductBrochureDto> ArchiveAsync(Guid productId, Guid brochureId, Guid adminId);
    Task<ProductBrochureDto> RetryIngestionAsync(
        Guid productId,
        Guid brochureId,
        Guid adminId,
        CancellationToken cancellationToken = default);
}
