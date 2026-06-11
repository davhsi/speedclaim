using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ProductDto>> GetAvailableProductsAsync()
    {
        var products = await _unitOfWork.InsuranceProducts.FindAsync(p => p.IsActive);
        return products.Select(p => new ProductDto(
            p.Id,
            p.ProductName,
            p.Domain,
            p.Uin,
            p.Description,
            p.MinAge,
            p.MaxAge,
            p.MinSumAssured,
            p.MaxSumAssured,
            p.MinTenureYears,
            p.MaxTenureYears,
            p.WaitingPeriodDays,
            p.AllowsFamilyFloater,
            p.MaxFamilyMembers,
            p.IsActive
        ));
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, string adminId)
    {
        var product = new InsuranceProduct
        {
            ProductName = request.ProductName,
            Domain = request.Domain,
            Uin = request.Uin,
            Description = request.Description,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            MinSumAssured = request.MinSumAssured,
            MaxSumAssured = request.MaxSumAssured,
            MinTenureYears = request.MinTenureYears,
            MaxTenureYears = request.MaxTenureYears,
            WaitingPeriodDays = request.WaitingPeriodDays,
            AllowsFamilyFloater = request.AllowsFamilyFloater,
            MaxFamilyMembers = request.MaxFamilyMembers,
            IsActive = true,
            CreatedById = Guid.Parse(adminId),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.InsuranceProducts.AddAsync(product);
        await _unitOfWork.CompleteAsync();

        return new ProductDto(
            product.Id,
            product.ProductName,
            product.Domain,
            product.Uin,
            product.Description,
            product.MinAge,
            product.MaxAge,
            product.MinSumAssured,
            product.MaxSumAssured,
            product.MinTenureYears,
            product.MaxTenureYears,
            product.WaitingPeriodDays,
            product.AllowsFamilyFloater,
            product.MaxFamilyMembers,
            product.IsActive
        );
    }

    public async Task UpdatePremiumRateTableAsync(string productId, UpdatePremiumRatesRequest request, string adminId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new Exception("Product not found");

        // Remove old rates
        var existingRates = await _unitOfWork.PremiumRateTables.FindAsync(r => r.ProductId == pId);
        foreach (var rate in existingRates)
        {
            _unitOfWork.PremiumRateTables.Delete(rate);
        }

        // Add new rates
        foreach (var rateDto in request.Rates)
        {
            var rate = new PremiumRateTable
            {
                ProductId = pId,
                AgeMin = rateDto.AgeMin,
                AgeMax = rateDto.AgeMax,
                SumAssuredMin = rateDto.SumAssuredMin,
                SumAssuredMax = rateDto.SumAssuredMax,
                AnnualPremium = rateDto.AnnualPremium,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.PremiumRateTables.AddAsync(rate);
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task ConfigureDocumentRequirementsAsync(string productId, UpdateDocumentRequirementsRequest request, string adminId)
    {
        foreach (var reqDto in request.Requirements)
        {
            var docReq = new DocumentRequirement
            {
                EntityType = reqDto.EntityType,
                Domain = reqDto.Domain,
                DocumentKey = reqDto.DocumentKey,
                Label = reqDto.Label,
                Description = reqDto.Description,
                IsMandatory = reqDto.IsMandatory,
                IsActive = reqDto.IsActive,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.DocumentRequirements.AddAsync(docReq);
        }
        
        await _unitOfWork.CompleteAsync();
    }

    public async Task<ProductDto> GetByIdAsync(string productId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new KeyNotFoundException("Product not found.");

        return new ProductDto(
            product.Id, product.ProductName, product.Domain, product.Uin, product.Description,
            product.MinAge, product.MaxAge, product.MinSumAssured, product.MaxSumAssured,
            product.MinTenureYears, product.MaxTenureYears, product.WaitingPeriodDays,
            product.AllowsFamilyFloater, product.MaxFamilyMembers, product.IsActive);
    }

    public async Task ToggleProductStatusAsync(string productId, bool isActive, string adminId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new Exception("Product not found");

        product.IsActive = isActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }
}
