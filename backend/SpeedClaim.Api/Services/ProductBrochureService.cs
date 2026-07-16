using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public sealed class ProductBrochureService : IProductBrochureService
{
    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IBrochureIngestionClient _ingestionClient;
    private readonly AiServiceOptions _options;

    public ProductBrochureService(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        IBrochureIngestionClient ingestionClient,
        IOptions<AiServiceOptions> options)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _ingestionClient = ingestionClient;
        _options = options.Value;
    }

    public async Task<ProductBrochureDto> UploadAsync(
        Guid productId,
        UploadProductBrochureRequest request,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductExistsAsync(productId);
        ValidateUpload(request);

        var existing = (await _unitOfWork.ProductBrochures.FindAsync(x => x.ProductId == productId)).ToList();
        var version = ResolveVersion(request.Version, existing);
        var originalFilename = SanitizeFilename(request.File.FileName);

        await using var uploadedStream = request.File.OpenReadStream();
        await using var content = new MemoryStream((int)request.File.Length);
        await uploadedStream.CopyToAsync(content, cancellationToken);
        ValidatePdfMagic(content);
        var contentHash = Convert.ToHexString(SHA256.HashData(content.ToArray())).ToLowerInvariant();

        if (existing.Any(x => string.Equals(x.ContentHash, contentHash, StringComparison.Ordinal)))
            throw new ConflictException("This PDF has already been uploaded for the product.");

        var brochure = new ProductBrochure
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Version = version,
            OriginalFilename = originalFilename,
            MimeType = "application/pdf",
            FileSizeKb = checked((int)Math.Ceiling(request.File.Length / 1024d)),
            ContentHash = contentHash,
            Status = ProductBrochureStatus.Processing,
            EffectiveFrom = request.EffectiveFrom,
            CreatedById = adminId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        content.Position = 0;
        brochure.BlobPath = await _storageService.UploadFileAsync(
            content,
            originalFilename,
            $"product-brochures/{productId:N}/{brochure.Id:N}");

        try
        {
            await _unitOfWork.ProductBrochures.AddAsync(brochure);
            await AddAuditAsync(brochure, adminId, "BrochureUploaded");
            await _unitOfWork.CompleteAsync();
        }
        catch
        {
            await _storageService.DeleteFileAsync(brochure.BlobPath);
            throw;
        }

        await RunIngestionAsync(brochure, adminId, cancellationToken);
        return Map(brochure);
    }

    public async Task<IReadOnlyList<ProductBrochureDto>> ListAsync(Guid productId)
    {
        await EnsureProductExistsAsync(productId);
        var brochures = await _unitOfWork.ProductBrochures.FindAsync(x => x.ProductId == productId);
        return brochures
            .OrderByDescending(x => ParseVersion(x.Version))
            .ThenByDescending(x => x.CreatedAt)
            .Select(Map)
            .ToList();
    }

    public async Task<ProductBrochureDto> GetAsync(Guid productId, Guid brochureId)
        => Map(await GetBrochureAsync(productId, brochureId));

    public async Task<ProductBrochureDto> PublishAsync(Guid productId, Guid brochureId, Guid adminId)
    {
        var brochure = await GetBrochureAsync(productId, brochureId);
        if (brochure.Status != ProductBrochureStatus.Ready)
            throw new ConflictException("Only a successfully ingested brochure can be published.");

        var hasPublishedVersion = await _unitOfWork.ProductBrochures.AnyAsync(x =>
            x.ProductId == productId &&
            x.Id != brochureId &&
            x.Status == ProductBrochureStatus.Published);
        if (hasPublishedVersion)
            throw new ConflictException("Archive the currently published brochure before publishing a new version.");

        brochure.Status = ProductBrochureStatus.Published;
        brochure.PublishedById = adminId;
        brochure.PublishedAt = DateTimeOffset.UtcNow;
        brochure.IngestionErrorCode = null;
        _unitOfWork.ProductBrochures.Update(brochure);
        await AddAuditAsync(brochure, adminId, "BrochurePublished");
        await _unitOfWork.CompleteAsync();
        return Map(brochure);
    }

    public async Task<ProductBrochureDto> ArchiveAsync(Guid productId, Guid brochureId, Guid adminId)
    {
        var brochure = await GetBrochureAsync(productId, brochureId);
        if (brochure.Status is not (ProductBrochureStatus.Ready or ProductBrochureStatus.Published))
            throw new ConflictException("Only a ready or published brochure can be archived.");

        brochure.Status = ProductBrochureStatus.Archived;
        brochure.EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow);
        _unitOfWork.ProductBrochures.Update(brochure);
        await AddAuditAsync(brochure, adminId, "BrochureArchived");
        await _unitOfWork.CompleteAsync();
        return Map(brochure);
    }

    public async Task<ProductBrochureDto> RetryIngestionAsync(
        Guid productId,
        Guid brochureId,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var brochure = await GetBrochureAsync(productId, brochureId);
        if (brochure.Status != ProductBrochureStatus.Failed)
            throw new ConflictException("Only a failed brochure ingestion can be retried.");

        brochure.Status = ProductBrochureStatus.Processing;
        brochure.IngestionErrorCode = null;
        _unitOfWork.ProductBrochures.Update(brochure);
        await AddAuditAsync(brochure, adminId, "BrochureIngestionRetried");
        await _unitOfWork.CompleteAsync();

        await RunIngestionAsync(brochure, adminId, cancellationToken);
        return Map(brochure);
    }

    private async Task RunIngestionAsync(
        ProductBrochure brochure,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestId = Guid.NewGuid();
            var result = await _ingestionClient.IngestAsync(
                new BrochureIngestionRequest(
                    requestId,
                    brochure.Id,
                    brochure.ProductId,
                    brochure.Version,
                    brochure.BlobPath,
                    brochure.ContentHash),
                cancellationToken);

            if (result.RequestId != requestId || result.DocumentId != brochure.Id)
                throw new BrochureIngestionException("invalid_ingestion_response", "The AI service returned mismatched identifiers.");

            brochure.Status = ProductBrochureStatus.Ready;
            brochure.PageCount = result.PageCount;
            brochure.ParentChunkCount = result.ParentChunkCount;
            brochure.ChildChunkCount = result.ChildChunkCount;
            brochure.EmbeddingProvider = result.EmbeddingProvider;
            brochure.EmbeddingModel = result.EmbeddingModel;
            brochure.EmbeddingDimension = result.EmbeddingDimension;
            brochure.IngestionErrorCode = null;
            _unitOfWork.ProductBrochures.Update(brochure);
            await AddAuditAsync(brochure, adminId, "BrochureIngestionSucceeded");
        }
        catch (Exception exception)
        {
            brochure.Status = ProductBrochureStatus.Failed;
            brochure.IngestionErrorCode = NormalizeErrorCode(
                exception is BrochureIngestionException ingestionException
                    ? ingestionException.ErrorCode
                    : exception is OperationCanceledException
                        ? "ai_ingestion_timeout"
                        : "ai_ingestion_unavailable");
            brochure.PageCount = null;
            brochure.ParentChunkCount = null;
            brochure.ChildChunkCount = null;
            brochure.EmbeddingProvider = null;
            brochure.EmbeddingModel = null;
            brochure.EmbeddingDimension = null;
            _unitOfWork.ProductBrochures.Update(brochure);
            await AddAuditAsync(brochure, adminId, "BrochureIngestionFailed");
        }

        await _unitOfWork.CompleteAsync();
    }

    private void ValidateUpload(UploadProductBrochureRequest request)
    {
        if (request.File is null || request.File.Length == 0)
            throw new ValidationException("A non-empty brochure PDF is required.");
        if (request.File.Length > _options.BrochureMaxFileSizeBytes)
            throw new ValidationException($"Brochure PDF exceeds the {_options.BrochureMaxFileSizeBytes / (1024 * 1024)}MB limit.");
        if (!string.Equals(Path.GetExtension(request.File.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Only PDF brochure files are allowed.");
        if (!string.Equals(request.File.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("The brochure content type must be application/pdf.");

        var filename = SanitizeFilename(request.File.FileName);
        if (filename.Length is 0 or > 255)
            throw new ValidationException("The brochure filename must contain between 1 and 255 characters.");
    }

    private static void ValidatePdfMagic(MemoryStream content)
    {
        if (content.Length < PdfMagic.Length || !content.GetBuffer().AsSpan(0, PdfMagic.Length).SequenceEqual(PdfMagic))
            throw new ValidationException("The uploaded file is not a valid PDF.");
    }

    private static string SanitizeFilename(string filename)
        => Path.GetFileName(filename.Replace('\\', '/')).Trim();

    private static string ResolveVersion(string? requestedVersion, IReadOnlyCollection<ProductBrochure> existing)
    {
        var highest = existing.Select(x => ParseVersion(x.Version)).DefaultIfEmpty(0).Max();
        if (requestedVersion is null)
            return checked(highest + 1).ToString();

        var normalized = requestedVersion.Trim();
        if (!int.TryParse(normalized, out var value) || value <= 0 || value > 999999)
            throw new ValidationException("Version must be a positive whole number up to 999999.");
        if (value <= highest)
            throw new ConflictException($"Brochure version must be greater than the current version {highest}.");
        return value.ToString();
    }

    private static int ParseVersion(string version)
        => int.TryParse(version, out var value) ? value : 0;

    private async Task EnsureProductExistsAsync(Guid productId)
    {
        if (await _unitOfWork.InsuranceProducts.GetByIdAsync(productId) is null)
            throw new NotFoundException("Product not found.");
    }

    private async Task<ProductBrochure> GetBrochureAsync(Guid productId, Guid brochureId)
    {
        var brochure = await _unitOfWork.ProductBrochures.GetByIdAsync(brochureId);
        if (brochure is null || brochure.ProductId != productId)
            throw new NotFoundException("Product brochure not found.");
        return brochure;
    }

    private async Task AddAuditAsync(ProductBrochure brochure, Guid adminId, string action)
    {
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            EntityType = "ProductBrochure",
            EntityId = brochure.Id,
            Action = action,
            NewValue = JsonSerializer.Serialize(new
            {
                brochure.ProductId,
                brochure.Version,
                status = brochure.Status.ToString(),
                brochure.ContentHash,
                brochure.IngestionErrorCode
            }),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string NormalizeErrorCode(string errorCode)
    {
        var normalized = new string(errorCode
            .Where(character => char.IsAsciiLetterOrDigit(character) || character == '_')
            .Take(100)
            .ToArray());
        return string.IsNullOrWhiteSpace(normalized)
            ? "ai_ingestion_failed"
            : normalized.ToLowerInvariant();
    }

    private static ProductBrochureDto Map(ProductBrochure brochure)
        => new(
            brochure.Id,
            brochure.ProductId,
            brochure.Version,
            brochure.OriginalFilename,
            brochure.BlobPath,
            brochure.MimeType,
            brochure.FileSizeKb,
            brochure.ContentHash,
            brochure.Status,
            brochure.EffectiveFrom,
            brochure.EffectiveTo,
            brochure.PageCount,
            brochure.ParentChunkCount,
            brochure.ChildChunkCount,
            brochure.EmbeddingProvider,
            brochure.EmbeddingModel,
            brochure.EmbeddingDimension,
            brochure.IngestionErrorCode,
            brochure.CreatedById,
            brochure.PublishedById,
            brochure.CreatedAt,
            brochure.PublishedAt);
}
