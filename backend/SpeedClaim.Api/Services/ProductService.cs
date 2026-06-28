using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Exceptions;
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

    private static ProductDto MapProduct(InsuranceProduct product)
    {
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

    public async Task<IEnumerable<ProductDto>> GetAvailableProductsAsync()
    {
        var products = await _unitOfWork.InsuranceProducts.FindAsync(p => p.IsActive);
        return products.Select(MapProduct);
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _unitOfWork.InsuranceProducts.GetAllAsync();
        return products.Select(MapProduct);
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

        return MapProduct(product);
    }

    public async Task<IEnumerable<PremiumRateDto>> GetPremiumRatesAsync(string productId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new NotFoundException("Product not found");

        var rates = await _unitOfWork.PremiumRateTables.FindAsync(r => r.ProductId == pId);
        return rates
            .OrderBy(r => r.AgeMin)
            .ThenBy(r => r.SumAssuredMin)
            .Select(r => new PremiumRateDto(
                r.AgeMin,
                r.AgeMax,
                r.SumAssuredMin,
                r.SumAssuredMax,
                r.AnnualPremium));
    }

    public async Task UpdatePremiumRateTableAsync(string productId, UpdatePremiumRatesRequest request, string adminId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new NotFoundException("Product not found");

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
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new NotFoundException("Product not found");

        var existing = await _unitOfWork.DocumentRequirements.FindAsync(d => d.ProductId == pId);
        foreach (var old in existing)
            _unitOfWork.DocumentRequirements.Delete(old);

        foreach (var reqDto in request.Requirements)
        {
            await _unitOfWork.DocumentRequirements.AddAsync(new DocumentRequirement
            {
                ProductId = pId,
                EntityType = reqDto.EntityType,
                Domain = reqDto.Domain,
                DocumentKey = reqDto.DocumentKey,
                Label = reqDto.Label,
                Description = reqDto.Description,
                IsMandatory = reqDto.IsMandatory,
                IsActive = reqDto.IsActive,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<DocumentRequirementResponseDto>> GetDocumentRequirementsAsync(string productId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new NotFoundException("Product not found");

        var requirements = await _unitOfWork.DocumentRequirements.FindAsync(d => d.ProductId == pId);
        return requirements.Select(d => new DocumentRequirementResponseDto(
            d.Id, d.ProductId, d.EntityType, d.Domain, d.DocumentKey,
            d.Label, d.Description, d.IsMandatory, d.IsActive));
    }

    public async Task<ProductDto> GetByIdAsync(string productId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new NotFoundException("Product not found.");

        return MapProduct(product);
    }

    public async Task ToggleProductStatusAsync(string productId, bool isActive, string adminId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new NotFoundException("Product not found");

        product.IsActive = isActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.CompleteAsync();
    }
}
