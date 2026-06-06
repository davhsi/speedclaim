using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Catalog;

namespace SpeedClaim.Api.Interfaces;

public interface IProductService
{
    Task<SpeedClaim.Api.Dtos.Common.PagedResponse<ProductDto>> GetAllProductsAsync(int pageNumber, int pageSize);
    Task<SpeedClaim.Api.Dtos.Common.PagedResponse<ProductDto>> GetProductsByDomainAsync(string domain, int pageNumber, int pageSize);
    Task<ProductDto> GetProductByIdAsync(Guid id);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task DeleteProductAsync(Guid id);
}
