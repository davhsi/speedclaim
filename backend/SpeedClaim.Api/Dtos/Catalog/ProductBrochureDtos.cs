using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Dtos.Catalog;

public sealed class UploadProductBrochureRequest
{
    public IFormFile File { get; set; } = null!;
    public DateOnly EffectiveFrom { get; set; }
    public string? Version { get; set; }
}

public record ProductBrochureDto(
    Guid Id,
    Guid ProductId,
    string Version,
    string OriginalFilename,
    string BlobPath,
    string MimeType,
    int FileSizeKb,
    string ContentHash,
    ProductBrochureStatus Status,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int? PageCount,
    int? ParentChunkCount,
    int? ChildChunkCount,
    string? EmbeddingProvider,
    string? EmbeddingModel,
    int? EmbeddingDimension,
    string? IngestionErrorCode,
    Guid CreatedById,
    Guid? PublishedById,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);

public record BrochureIngestionRequest(
    Guid RequestId,
    Guid BrochureId,
    Guid ProductId,
    string Version,
    string BlobPath,
    string ContentHash);

public record BrochureIngestionResponse(
    Guid RequestId,
    Guid DocumentId,
    string Status,
    int PageCount,
    int ParentChunkCount,
    int ChildChunkCount,
    string EmbeddingProvider,
    string EmbeddingModel,
    int EmbeddingDimension);
