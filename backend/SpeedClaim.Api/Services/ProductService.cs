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
    private readonly SpeedClaimDbContext _context;
    private readonly IMapper _mapper;

    public ProductService(SpeedClaimDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _context.InsuranceProducts.ToListAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByDomainAsync(string domain)
    {
        var products = await _context.InsuranceProducts
            .Where(p => p.Domain.ToUpper() == domain.ToUpper())
            .ToListAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id)
    {
        var product = await _context.InsuranceProducts.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found.");
            
        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        // Check if code/domain combo exists
        var exists = await _context.InsuranceProducts
            .AnyAsync(p => p.Code == request.Code && p.Domain == request.Domain.ToUpper());
            
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

        _context.InsuranceProducts.Add(product);
        await _context.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _context.InsuranceProducts.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.MaxCoverage = request.MaxCoverage;
        product.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return _mapper.Map<ProductDto>(product);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _context.InsuranceProducts.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found.");

        // Instead of hard delete, we set IsActive to false since there is no deleted_at column
        product.IsActive = false;
        await _context.SaveChangesAsync();
    }
}
