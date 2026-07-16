using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using SpeedClaim.Api.Configuration;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class ProductBrochureServiceTests
{
    private static readonly byte[] ValidPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nsynthetic brochure text");
    private Mock<IUnitOfWork> _unitOfWork = null!;
    private Mock<IRepository<InsuranceProduct>> _products = null!;
    private Mock<IRepository<ProductBrochure>> _brochures = null!;
    private Mock<IRepository<AuditLog>> _audits = null!;
    private Mock<IStorageService> _storage = null!;
    private Mock<IBrochureIngestionClient> _ingestion = null!;
    private List<ProductBrochure> _existing = null!;
    private List<AuditLog> _auditEntries = null!;
    private Guid _productId;
    private Guid _adminId;
    private ProductBrochureService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _productId = Guid.NewGuid();
        _adminId = Guid.NewGuid();
        _existing = [];
        _auditEntries = [];
        _unitOfWork = new Mock<IUnitOfWork>();
        _products = new Mock<IRepository<InsuranceProduct>>();
        _brochures = new Mock<IRepository<ProductBrochure>>();
        _audits = new Mock<IRepository<AuditLog>>();
        _storage = new Mock<IStorageService>();
        _ingestion = new Mock<IBrochureIngestionClient>();

        _unitOfWork.SetupGet(x => x.InsuranceProducts).Returns(_products.Object);
        _unitOfWork.SetupGet(x => x.ProductBrochures).Returns(_brochures.Object);
        _unitOfWork.SetupGet(x => x.AuditLogs).Returns(_audits.Object);
        _unitOfWork.Setup(x => x.CompleteAsync()).ReturnsAsync(1);
        _products.Setup(x => x.GetByIdAsync(_productId))
            .ReturnsAsync(new InsuranceProduct { Id = _productId });
        _brochures.Setup(x => x.FindAsync(It.IsAny<Expression<Func<ProductBrochure, bool>>>()))
            .ReturnsAsync(() => _existing);
        _brochures.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<ProductBrochure, bool>>>()))
            .ReturnsAsync(false);
        _brochures.Setup(x => x.AddAsync(It.IsAny<ProductBrochure>()))
            .Callback<ProductBrochure>(item => _existing.Add(item))
            .Returns(Task.CompletedTask);
        _audits.Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .Callback<AuditLog>(item => _auditEntries.Add(item))
            .Returns(Task.CompletedTask);
        _storage.Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("uploads/product-brochures/brochure.pdf");
        _storage.Setup(x => x.DeleteFileAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _ingestion.Setup(x => x.IngestAsync(It.IsAny<BrochureIngestionRequest>(), It.IsAny<CancellationToken>()))
            .Returns((BrochureIngestionRequest request, CancellationToken _) => Task.FromResult(Succeeded(request)));

        _service = new ProductBrochureService(
            _unitOfWork.Object,
            _storage.Object,
            _ingestion.Object,
            Options.Create(new AiServiceOptions()));
    }

    [Test]
    public async Task UploadAsync_ValidPdf_StoresAndMarksReady()
    {
        var result = await _service.UploadAsync(_productId, Request(), _adminId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Version, Is.EqualTo("1"));
            Assert.That(result.Status, Is.EqualTo(ProductBrochureStatus.Ready));
            Assert.That(result.PageCount, Is.EqualTo(12));
            Assert.That(result.ChildChunkCount, Is.EqualTo(22));
            Assert.That(result.BlobPath, Does.StartWith("uploads/product-brochures/"));
        });
        _unitOfWork.Verify(x => x.CompleteAsync(), Times.Exactly(2));
        Assert.That(_auditEntries.Select(x => x.Action), Is.EquivalentTo(new[]
        {
            "BrochureUploaded", "BrochureIngestionSucceeded"
        }));
    }

    [Test]
    public async Task UploadAsync_NoVersion_AssignsNextNumericVersion()
    {
        _existing.Add(Brochure(ProductBrochureStatus.Archived, "2"));

        var result = await _service.UploadAsync(_productId, Request(), _adminId);

        Assert.That(result.Version, Is.EqualTo("3"));
    }

    [Test]
    public void UploadAsync_NonMonotonicVersion_IsRejectedBeforeStorage()
    {
        _existing.Add(Brochure(ProductBrochureStatus.Archived, "3"));

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.UploadAsync(_productId, Request(version: "2"), _adminId));
        _storage.Verify(
            x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public void UploadAsync_DuplicateContent_IsRejectedBeforeStorage()
    {
        var existing = Brochure(ProductBrochureStatus.Archived, "1");
        existing.ContentHash = Convert.ToHexString(SHA256.HashData(ValidPdf)).ToLowerInvariant();
        _existing.Add(existing);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.UploadAsync(_productId, Request(), _adminId));
        _storage.Verify(
            x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestCase("text/plain", "%PDF-1.7")]
    [TestCase("application/pdf", "not a pdf")]
    public void UploadAsync_InvalidPdf_IsRejected(string mimeType, string content)
    {
        var request = Request(bytes: Encoding.ASCII.GetBytes(content), contentType: mimeType);

        Assert.ThrowsAsync<ValidationException>(() =>
            _service.UploadAsync(_productId, request, _adminId));
    }

    [Test]
    public async Task UploadAsync_IngestionFailure_PersistsRetryableFailedState()
    {
        _ingestion.Setup(x => x.IngestAsync(It.IsAny<BrochureIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BrochureIngestionException("image_only_pdf", "rejected"));

        var result = await _service.UploadAsync(_productId, Request(), _adminId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ProductBrochureStatus.Failed));
            Assert.That(result.IngestionErrorCode, Is.EqualTo("image_only_pdf"));
            Assert.That(result.PageCount, Is.Null);
        });
        Assert.That(_auditEntries.Last().Action, Is.EqualTo("BrochureIngestionFailed"));
    }

    [Test]
    public async Task RetryIngestionAsync_FailedBrochure_BecomesReadyWithoutReplacingFile()
    {
        var brochure = Brochure(ProductBrochureStatus.Failed, "1");
        brochure.IngestionErrorCode = "ai_ingestion_unavailable";
        _brochures.Setup(x => x.GetByIdAsync(brochure.Id)).ReturnsAsync(brochure);

        var result = await _service.RetryIngestionAsync(_productId, brochure.Id, _adminId);

        Assert.That(result.Status, Is.EqualTo(ProductBrochureStatus.Ready));
        Assert.That(result.IngestionErrorCode, Is.Null);
        _storage.Verify(
            x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _unitOfWork.Verify(x => x.CompleteAsync(), Times.Exactly(2));
    }

    [Test]
    public void RetryIngestionAsync_ReadyBrochure_IsRejected()
    {
        var brochure = Brochure(ProductBrochureStatus.Ready, "1");
        _brochures.Setup(x => x.GetByIdAsync(brochure.Id)).ReturnsAsync(brochure);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.RetryIngestionAsync(_productId, brochure.Id, _adminId));
    }

    [TestCase(ProductBrochureStatus.Failed)]
    [TestCase(ProductBrochureStatus.Processing)]
    [TestCase(ProductBrochureStatus.Archived)]
    public void PublishAsync_NonReadyBrochure_IsRejected(ProductBrochureStatus status)
    {
        var brochure = Brochure(status, "1");
        _brochures.Setup(x => x.GetByIdAsync(brochure.Id)).ReturnsAsync(brochure);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.PublishAsync(_productId, brochure.Id, _adminId));
    }

    [Test]
    public void PublishAsync_WhenAnotherVersionIsPublished_RequiresExplicitArchive()
    {
        var brochure = Brochure(ProductBrochureStatus.Ready, "2");
        _brochures.Setup(x => x.GetByIdAsync(brochure.Id)).ReturnsAsync(brochure);
        _brochures.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<ProductBrochure, bool>>>()))
            .ReturnsAsync(true);

        var exception = Assert.ThrowsAsync<ConflictException>(() =>
            _service.PublishAsync(_productId, brochure.Id, _adminId));

        Assert.That(exception!.Message, Does.Contain("Archive"));
    }

    [Test]
    public async Task PublishAsync_ReadyBrochure_MarksPublished()
    {
        var brochure = Brochure(ProductBrochureStatus.Ready, "1");
        _brochures.Setup(x => x.GetByIdAsync(brochure.Id)).ReturnsAsync(brochure);

        var result = await _service.PublishAsync(_productId, brochure.Id, _adminId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ProductBrochureStatus.Published));
            Assert.That(result.PublishedById, Is.EqualTo(_adminId));
            Assert.That(result.PublishedAt, Is.Not.Null);
        });
        Assert.That(_auditEntries.Single().Action, Is.EqualTo("BrochurePublished"));
    }

    [Test]
    public async Task ArchiveAsync_PublishedBrochure_ClosesEffectivePeriod()
    {
        var brochure = Brochure(ProductBrochureStatus.Published, "1");
        _brochures.Setup(x => x.GetByIdAsync(brochure.Id)).ReturnsAsync(brochure);

        var result = await _service.ArchiveAsync(_productId, brochure.Id, _adminId);

        Assert.That(result.Status, Is.EqualTo(ProductBrochureStatus.Archived));
        Assert.That(result.EffectiveTo, Is.EqualTo(DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Test]
    public void ArchiveAsync_FailedBrochure_IsRejectedAndRemainsRetryable()
    {
        var brochure = Brochure(ProductBrochureStatus.Failed, "1");
        _brochures.Setup(x => x.GetByIdAsync(brochure.Id)).ReturnsAsync(brochure);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.ArchiveAsync(_productId, brochure.Id, _adminId));
        Assert.That(brochure.Status, Is.EqualTo(ProductBrochureStatus.Failed));
    }

    [Test]
    public async Task ListAsync_ReturnsNewestVersionFirst()
    {
        _existing.AddRange([
            Brochure(ProductBrochureStatus.Archived, "1"),
            Brochure(ProductBrochureStatus.Ready, "3"),
            Brochure(ProductBrochureStatus.Archived, "2")
        ]);

        var result = await _service.ListAsync(_productId);

        Assert.That(result.Select(x => x.Version), Is.EqualTo(new[] { "3", "2", "1" }));
    }

    [Test]
    public void GetAsync_BrochureBelongsToAnotherProduct_ReturnsNotFound()
    {
        var brochure = Brochure(ProductBrochureStatus.Ready, "1");
        brochure.ProductId = Guid.NewGuid();
        _brochures.Setup(x => x.GetByIdAsync(brochure.Id)).ReturnsAsync(brochure);

        Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(_productId, brochure.Id));
    }

    private static BrochureIngestionResponse Succeeded(BrochureIngestionRequest request)
        => new(request.RequestId, request.BrochureId, "Succeeded", 12, 8, 22, "FastEmbed", "BAAI/bge-small-en-v1.5", 384);

    private UploadProductBrochureRequest Request(
        string? version = null,
        byte[]? bytes = null,
        string contentType = "application/pdf")
    {
        bytes ??= ValidPdf;
        var stream = new MemoryStream(bytes);
        var file = new FormFile(stream, 0, bytes.Length, "file", "brochure.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
        return new UploadProductBrochureRequest
        {
            File = file,
            EffectiveFrom = new DateOnly(2026, 7, 1),
            Version = version
        };
    }

    private ProductBrochure Brochure(ProductBrochureStatus status, string version)
        => new()
        {
            Id = Guid.NewGuid(),
            ProductId = _productId,
            Version = version,
            OriginalFilename = "brochure.pdf",
            BlobPath = "uploads/product-brochures/brochure.pdf",
            MimeType = "application/pdf",
            FileSizeKb = 1,
            ContentHash = new string('a', 64),
            Status = status,
            EffectiveFrom = new DateOnly(2026, 7, 1),
            CreatedById = _adminId
        };
}
