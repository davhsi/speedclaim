using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Interfaces;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize]
public class ProductController : BaseApiController
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [AllowAnonymous] // or Authorize(Roles = "Customer, Agent") based on requirement
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var result = await _productService.GetAvailableProductsAsync();
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(string id)
    {
        var result = await _productService.GetByIdAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        var result = await _productService.CreateProductAsync(request, adminId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/rates")]
    public async Task<IActionResult> UpdateRates(string id, [FromBody] UpdatePremiumRatesRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _productService.UpdatePremiumRateTableAsync(id, request, adminId);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/documents")]
    public async Task<IActionResult> ConfigureDocuments(string id, [FromBody] UpdateDocumentRequirementsRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _productService.ConfigureDocumentRequirementsAsync(id, request, adminId);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> ToggleStatus(string id, [FromBody] bool isActive)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _productService.ToggleProductStatusAsync(id, isActive, adminId);
        return Ok();
    }
}
