using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Catalog;

namespace SpeedClaim.Api.Interfaces;

public interface IProductService
{
    // Customer / Agent
    Task<IEnumerable<ProductDto>> GetAvailableProductsAsync();
    Task<ProductDto> GetByIdAsync(string productId);

    // Admin
    Task<ProductDto> CreateProductAsync(CreateProductRequest request, string adminId);
    Task UpdatePremiumRateTableAsync(string productId, UpdatePremiumRatesRequest request, string adminId);
    Task ConfigureDocumentRequirementsAsync(string productId, UpdateDocumentRequirementsRequest request, string adminId);
    Task ToggleProductStatusAsync(string productId, bool isActive, string adminId);
}
