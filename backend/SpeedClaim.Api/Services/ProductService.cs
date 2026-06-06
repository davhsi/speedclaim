using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SpeedClaim.Api.Dtos.Common.PagedResponse<ProductDto>> GetAllProductsAsync(int pageNumber, int pageSize)
    {
        var (products, totalCount) = await _unitOfWork.Products.GetPagedAsync(pageNumber, pageSize);
        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
        return new SpeedClaim.Api.Dtos.Common.PagedResponse<ProductDto>(productDtos, pageNumber, pageSize, totalCount);
    }

    public async Task<SpeedClaim.Api.Dtos.Common.PagedResponse<ProductDto>> GetProductsByDomainAsync(string domain, int pageNumber, int pageSize)
    {
        var (products, totalCount) = await _unitOfWork.Products.GetPagedAsync(pageNumber, pageSize, p => p.Domain.ToUpper() == domain.ToUpper());
        var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
        return new SpeedClaim.Api.Dtos.Common.PagedResponse<ProductDto>(productDtos, pageNumber, pageSize, totalCount);
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found.");
            
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        // Check if code/domain combo exists
        var existingProduct = await _unitOfWork.Products
            .SingleOrDefaultAsync(p => p.Code == request.Code && p.Domain == request.Domain.ToUpper());
        var exists = existingProduct != null;
            
        if (exists)
            throw new InvalidOperationException($"Product with code {request.Code} and domain {request.Domain} already exists.");

        var product = new InsuranceProduct
        {
            Id = Guid.NewGuid(),
            Code = request.Code,
            Name = request.Name,
            Domain = request.Domain.ToUpper(),
            Description = request.Description,
            MaxCoverage = request.MaxCoverage,
            IsActive = true
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.CompleteAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.MaxCoverage = request.MaxCoverage;
        product.IsActive = request.IsActive;

        _unitOfWork.Products.Update(product);
        await _unitOfWork.CompleteAsync();
        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found.");

        // Instead of hard delete, we set IsActive to false since there is no deleted_at column
        product.IsActive = false;
        _unitOfWork.Products.Update(product);
        await _unitOfWork.CompleteAsync();
    }
}
