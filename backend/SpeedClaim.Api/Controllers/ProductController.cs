using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Interfaces;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    #region Public Endpoints

    /// <summary>Get all active insurance products available for purchase</summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
    public async Task<IActionResult> GetProducts()
    {
        var result = await _productService.GetAvailableProductsAsync();
        return Ok(result);
    }

    /// <summary>Get a single insurance product by ID including its configuration</summary>
    /// <param name="id">Product ID</param>
    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProductById(string id)
    {
        var result = await _productService.GetByIdAsync(id);
        return Ok(result);
    }

    #endregion

    #region Admin Endpoints

    /// <summary>Admin — get all insurance products, including inactive products</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("~/api/v{version:apiVersion}/admin/products")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
    public async Task<IActionResult> GetAllProductsForAdmin()
    {
        var result = await _productService.GetAllProductsAsync();
        return Ok(result);
    }

    /// <summary>Admin — create a new insurance product</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), 200)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        var result = await _productService.CreateProductAsync(request, adminId);
        return Ok(result);
    }

    /// <summary>Admin — update a product's editable configuration. UIN and domain are immutable.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateProductRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        var result = await _productService.UpdateProductAsync(id, request, adminId);
        return Ok(result);
    }

    /// <summary>Admin — get the premium rate table for a product</summary>
    /// <param name="id">Product ID</param>
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}/rates")]
    [ProducesResponseType(typeof(IEnumerable<PremiumRateDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetRates(string id)
    {
        var result = await _productService.GetPremiumRatesAsync(id);
        return Ok(result);
    }

    /// <summary>Admin — replace the premium rate table for a product</summary>
    /// <remarks>All existing rates for the product are removed and replaced with the new set.</remarks>
    /// <param name="id">Product ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/rates")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateRates(string id, [FromBody] UpdatePremiumRatesRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _productService.UpdatePremiumRateTableAsync(id, request, adminId);
        return Ok(new { message = "Premium rate table updated." });
    }

    /// <summary>Get document requirements for a product</summary>
    /// <param name="id">Product ID</param>
    [AllowAnonymous]
    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(IEnumerable<DocumentRequirementResponseDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDocuments(string id)
    {
        var result = await _productService.GetDocumentRequirementsAsync(id);
        return Ok(result);
    }

    /// <summary>Admin — configure the document requirements for a product (replaces existing)</summary>
    /// <param name="id">Product ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/documents")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ConfigureDocuments(string id, [FromBody] UpdateDocumentRequirementsRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _productService.ConfigureDocumentRequirementsAsync(id, request, adminId);
        return Ok(new { message = "Document requirements configured." });
    }

    /// <summary>Admin — activate or deactivate an insurance product</summary>
    /// <remarks>Inactive products are hidden from public listings and cannot be used in new proposals.</remarks>
    /// <param name="id">Product ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleStatus(string id, [FromBody] bool isActive)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _productService.ToggleProductStatusAsync(id, isActive, adminId);
        return Ok(new { message = isActive ? "Product activated." : "Product deactivated." });
    }

    /// <summary>Admin — withdraw or restore a product from new sales without affecting existing policies</summary>
    /// <param name="id">Product ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/sale-availability")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ToggleSaleAvailability(string id, [FromBody] bool isAvailableForSale)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _productService.ToggleProductSaleAvailabilityAsync(id, isAvailableForSale, adminId);
        return Ok(new { message = isAvailableForSale ? "Product restored to sale." : "Product withdrawn from sale." });
    }

    #endregion
}
