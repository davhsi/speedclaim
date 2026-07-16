using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[Route("api/v{version:apiVersion}/products/{productId:guid}/brochures")]
[ProducesResponseType(500)]
public sealed class ProductBrochuresController : ControllerBase
{
    private readonly IProductBrochureService _brochureService;

    public ProductBrochuresController(IProductBrochureService brochureService)
    {
        _brochureService = brochureService;
    }

    /// <summary>Admin — upload one PDF as a new immutable brochure version and trigger ingestion.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductBrochureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upload(
        Guid productId,
        [FromForm] UploadProductBrochureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _brochureService.UploadAsync(productId, request, GetAdminId(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { productId, brochureId = result.Id, version = "1.0" }, result);
    }

    /// <summary>Admin — list all immutable brochure versions for a product.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductBrochureDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid productId)
        => Ok(await _brochureService.ListAsync(productId));

    /// <summary>Admin — get one brochure version.</summary>
    [HttpGet("{brochureId:guid}")]
    [ProducesResponseType(typeof(ProductBrochureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid productId, Guid brochureId)
        => Ok(await _brochureService.GetAsync(productId, brochureId));

    /// <summary>Admin — publish a successfully ingested brochure.</summary>
    [HttpPut("{brochureId:guid}/publish")]
    [ProducesResponseType(typeof(ProductBrochureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid productId, Guid brochureId)
        => Ok(await _brochureService.PublishAsync(productId, brochureId, GetAdminId()));

    /// <summary>Admin — archive a ready or published brochure without deleting its indexed content.</summary>
    [HttpPut("{brochureId:guid}/archive")]
    [ProducesResponseType(typeof(ProductBrochureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Archive(Guid productId, Guid brochureId)
        => Ok(await _brochureService.ArchiveAsync(productId, brochureId, GetAdminId()));

    /// <summary>Admin — retry idempotent ingestion for a failed brochure.</summary>
    [HttpPut("{brochureId:guid}/retry")]
    [ProducesResponseType(typeof(ProductBrochureDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Retry(
        Guid productId,
        Guid brochureId,
        CancellationToken cancellationToken)
        => Ok(await _brochureService.RetryIngestionAsync(
            productId,
            brochureId,
            GetAdminId(),
            cancellationToken));

    private Guid GetAdminId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var adminId)
            ? adminId
            : throw new UnauthorizedAccessException("Authenticated admin identifier is invalid.");
    }
}
