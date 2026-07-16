using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Models;

public class ProductBrochure
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Version { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public string BlobPath { get; set; } = string.Empty;
    public string MimeType { get; set; } = "application/pdf";
    public int FileSizeKb { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public ProductBrochureStatus Status { get; set; } = ProductBrochureStatus.Draft;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int? PageCount { get; set; }
    public int? ParentChunkCount { get; set; }
    public int? ChildChunkCount { get; set; }
    public string? EmbeddingProvider { get; set; }
    public string? EmbeddingModel { get; set; }
    public int? EmbeddingDimension { get; set; }
    public string? IngestionErrorCode { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? PublishedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }

    public virtual InsuranceProduct Product { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
    public virtual User? PublishedBy { get; set; }
}
