using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        var coverageOptions = ParseCoverageOptions(product.CoverageOptionsJson);
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
            product.IsActive,
            product.IsAvailableForSale,
            product.MotorVehicleType,
            coverageOptions,
            product.SumAssuredIncrement
        );
    }

    private static List<decimal> ParseCoverageOptions(string? value)
    {
        try { return JsonSerializer.Deserialize<List<decimal>>(value ?? "[]") ?? []; }
        catch (JsonException) { return []; }
    }

    private static List<decimal> NormalizeCoverageOptions(IEnumerable<decimal>? options)
        => (options ?? [])
            .Where(option => option > 0)
            .Distinct()
            .OrderBy(option => option)
            .ToList();

    private static void ValidateCoverageConfiguration(string domain, decimal min, decimal max, List<decimal> options, decimal? increment)
    {
        if (string.Equals(domain, "Health", StringComparison.OrdinalIgnoreCase))
        {
            if (options.Count == 0)
                throw new ValidationException("Health products require at least one selectable cover option.");
            return;
        }

        if (string.Equals(domain, "Life", StringComparison.OrdinalIgnoreCase))
        {
            if (min <= 0 || max < min)
                throw new ValidationException("Life product cover limits are invalid.");
            if (!increment.HasValue || increment.Value <= 0)
                throw new ValidationException("Life products require a positive sum assured increment.");
            return;
        }

        if (min <= 0 || max < min)
            throw new ValidationException("Motor IDV guardrails are invalid.");
    }

    public async Task<IEnumerable<ProductDto>> GetAvailableProductsAsync()
    {
        var products = await _unitOfWork.InsuranceProducts.FindAsync(p => p.IsActive && p.IsAvailableForSale);
        return products.Select(MapProduct);
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        var products = await _unitOfWork.InsuranceProducts.GetAllAsync();
        return products.Select(MapProduct);
    }

    private static readonly Dictionary<string, string> UinDomainCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Motor"] = "MO",
        ["Health"] = "HC",
        ["Life"] = "LI"
    };

    private async Task<string> GenerateUinAsync(string domain)
    {
        var code = UinDomainCodes.TryGetValue(domain, out var c) ? c : "XX";
        var prefix = $"UIN-{code}-{DateTime.UtcNow.Year}-";
        var existing = await _unitOfWork.InsuranceProducts.FindAsync(p => p.Uin.StartsWith(prefix));
        var maxSequence = existing
            .Select(p => int.TryParse(p.Uin[prefix.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return prefix + (maxSequence + 1).ToString("D4");
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request, string adminId)
    {
        var coverageOptions = NormalizeCoverageOptions(request.CoverageOptions);
        var minSumAssured = string.Equals(request.Domain, "Health", StringComparison.OrdinalIgnoreCase)
            ? coverageOptions.FirstOrDefault()
            : request.MinSumAssured;
        var maxSumAssured = string.Equals(request.Domain, "Health", StringComparison.OrdinalIgnoreCase)
            ? coverageOptions.LastOrDefault()
            : request.MaxSumAssured;
        ValidateCoverageConfiguration(request.Domain, minSumAssured, maxSumAssured, coverageOptions, request.SumAssuredIncrement);

        var product = new InsuranceProduct
        {
            ProductName = request.ProductName,
            Domain = request.Domain,
            Uin = await GenerateUinAsync(request.Domain),
            Description = request.Description,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            MinSumAssured = minSumAssured,
            MaxSumAssured = maxSumAssured,
            CoverageOptionsJson = JsonSerializer.Serialize(coverageOptions),
            SumAssuredIncrement = string.Equals(request.Domain, "Life", StringComparison.OrdinalIgnoreCase) ? request.SumAssuredIncrement : null,
            MinTenureYears = request.MinTenureYears,
            MaxTenureYears = request.MaxTenureYears,
            WaitingPeriodDays = request.WaitingPeriodDays,
            MotorVehicleType = string.Equals(request.Domain, "Motor", StringComparison.OrdinalIgnoreCase) ? request.MotorVehicleType : null,
            AllowsFamilyFloater = request.AllowsFamilyFloater,
            MaxFamilyMembers = request.MaxFamilyMembers,
            IsActive = true,
            IsAvailableForSale = true,
            CreatedById = Guid.Parse(adminId),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.InsuranceProducts.AddAsync(product);
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(adminId), EntityType = "InsuranceProduct", EntityId = product.Id,
            Action = "ProductCreated",
            NewValue = JsonSerializer.Serialize(new { productName = product.ProductName, domain = product.Domain }),
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CompleteAsync();

        return MapProduct(product);
    }

    public async Task<ProductDto> UpdateProductAsync(string productId, UpdateProductRequest request, string adminId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new NotFoundException("Product not found");

        if (string.IsNullOrWhiteSpace(request.ProductName) || string.IsNullOrWhiteSpace(request.Description))
            throw new ValidationException("Product name and description are required.");
        if (request.MinAge < 1 || request.MaxAge < request.MinAge)
            throw new ValidationException("Product age range is invalid.");
        var coverageOptions = NormalizeCoverageOptions(request.CoverageOptions);
        var minSumAssured = string.Equals(product.Domain, "Health", StringComparison.OrdinalIgnoreCase)
            ? coverageOptions.FirstOrDefault()
            : request.MinSumAssured;
        var maxSumAssured = string.Equals(product.Domain, "Health", StringComparison.OrdinalIgnoreCase)
            ? coverageOptions.LastOrDefault()
            : request.MaxSumAssured;
        ValidateCoverageConfiguration(product.Domain, minSumAssured, maxSumAssured, coverageOptions, request.SumAssuredIncrement);
        if (request.MinTenureYears <= 0 || request.MaxTenureYears < request.MinTenureYears)
            throw new ValidationException("Product tenure range is invalid.");
        if (request.WaitingPeriodDays < 0)
            throw new ValidationException("Waiting period cannot be negative.");
        if (request.AllowsFamilyFloater && request.MaxFamilyMembers < 2)
            throw new ValidationException("Family floater products require at least two family members.");
        if (string.Equals(product.Domain, "Motor", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.MotorVehicleType))
            throw new ValidationException("Motor products require a vehicle type.");

        var oldValue = JsonSerializer.Serialize(new
        {
            product.ProductName, product.Description, product.MinAge, product.MaxAge,
            product.MinSumAssured, product.MaxSumAssured, product.CoverageOptionsJson, product.SumAssuredIncrement, product.MinTenureYears,
            product.MaxTenureYears, product.WaitingPeriodDays, product.AllowsFamilyFloater,
            product.MaxFamilyMembers, product.MotorVehicleType
        });

        product.ProductName = request.ProductName.Trim();
        product.Description = request.Description.Trim();
        product.MinAge = request.MinAge;
        product.MaxAge = request.MaxAge;
        product.MinSumAssured = minSumAssured;
        product.MaxSumAssured = maxSumAssured;
        product.CoverageOptionsJson = JsonSerializer.Serialize(coverageOptions);
        product.SumAssuredIncrement = string.Equals(product.Domain, "Life", StringComparison.OrdinalIgnoreCase) ? request.SumAssuredIncrement : null;
        product.MinTenureYears = request.MinTenureYears;
        product.MaxTenureYears = request.MaxTenureYears;
        product.WaitingPeriodDays = request.WaitingPeriodDays;
        product.AllowsFamilyFloater = request.AllowsFamilyFloater;
        product.MaxFamilyMembers = request.AllowsFamilyFloater ? request.MaxFamilyMembers : 1;
        product.MotorVehicleType = string.Equals(product.Domain, "Motor", StringComparison.OrdinalIgnoreCase)
            ? request.MotorVehicleType
            : null;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(adminId), EntityType = "InsuranceProduct", EntityId = pId,
            Action = "ProductUpdated", OldValue = oldValue,
            NewValue = JsonSerializer.Serialize(new { product.ProductName, product.MinAge, product.MaxAge, product.MinSumAssured, product.MaxSumAssured, coverageOptions, product.SumAssuredIncrement, product.WaitingPeriodDays }),
            CreatedAt = DateTime.UtcNow
        });
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
        ValidatePremiumRates(product, request.Rates);

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
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(adminId), EntityType = "InsuranceProduct", EntityId = pId,
            Action = "PremiumRatesUpdated",
            NewValue = JsonSerializer.Serialize(new { productId = pId, rateCount = request.Rates.Count() }),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    private static void ValidatePremiumRates(InsuranceProduct product, IReadOnlyList<PremiumRateDto> rates)
    {
        if (rates.Count == 0)
            throw new ValidationException("At least one premium rate band is required.");

        for (var i = 0; i < rates.Count; i++)
        {
            var rate = rates[i];
            if (rate.AgeMin < 0 || rate.AgeMax < rate.AgeMin)
                throw new ValidationException("Each premium rate band must have a valid age range.");
            if (rate.SumAssuredMin <= 0 || rate.SumAssuredMax < rate.SumAssuredMin)
                throw new ValidationException("Each premium rate band must have a valid sum assured range.");
            if (rate.AnnualPremium <= 0)
                throw new ValidationException("Each premium rate band must have a positive annual premium.");
        }

        var isMotor = string.Equals(product.Domain, "Motor", StringComparison.OrdinalIgnoreCase);
        for (var i = 0; i < rates.Count; i++)
        {
            for (var j = i + 1; j < rates.Count; j++)
            {
                var left = rates[i];
                var right = rates[j];
                var sumsOverlap = RangesOverlap(left.SumAssuredMin, left.SumAssuredMax, right.SumAssuredMin, right.SumAssuredMax);
                var agesOverlap = isMotor || RangesOverlap(left.AgeMin, left.AgeMax, right.AgeMin, right.AgeMax);
                if (sumsOverlap && agesOverlap)
                    throw new ValidationException("Premium rate bands cannot overlap.");
            }
        }
    }

    private static bool RangesOverlap(decimal leftMin, decimal leftMax, decimal rightMin, decimal rightMax)
        => leftMin <= rightMax && rightMin <= leftMax;

    private static bool RangesOverlap(int leftMin, int leftMax, int rightMin, int rightMax)
        => leftMin <= rightMax && rightMin <= leftMax;

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

        if (!isActive)
            await EnsureProductCanBeDeactivatedAsync(pId);

        product.IsActive = isActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(), UserId = Guid.Parse(adminId), EntityType = "InsuranceProduct", EntityId = pId,
            Action = isActive ? "ProductActivated" : "ProductDeactivated",
            NewValue = JsonSerializer.Serialize(new { productName = product.ProductName }),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    public async Task ToggleProductSaleAvailabilityAsync(string productId, bool isAvailableForSale, string adminId)
    {
        var pId = Guid.Parse(productId);
        var product = await _unitOfWork.InsuranceProducts.GetByIdAsync(pId);
        if (product == null) throw new NotFoundException("Product not found");

        if (isAvailableForSale && !product.IsActive)
            throw new ConflictException("Archived products must be activated before they can be restored to sale.");

        product.IsAvailableForSale = isAvailableForSale;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(adminId),
            EntityType = "InsuranceProduct",
            EntityId = pId,
            Action = isAvailableForSale ? "ProductRestoredToSale" : "ProductWithdrawnFromSale",
            NewValue = JsonSerializer.Serialize(new { productName = product.ProductName }),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    private async Task EnsureProductCanBeDeactivatedAsync(Guid productId)
    {
        var activeProposalStatuses = new[]
        {
            Models.Enums.ProposalStatus.Submitted,
            Models.Enums.ProposalStatus.UnderReview,
            Models.Enums.ProposalStatus.DocumentsPending,
            Models.Enums.ProposalStatus.Approved
        };
        var activeProposals = await _unitOfWork.Proposals.FindAsync(p =>
            p.ProductId == productId && activeProposalStatuses.Contains(p.Status));

        var livePolicyStatuses = new[]
        {
            Models.Enums.PolicyStatus.Pending,
            Models.Enums.PolicyStatus.Active,
            Models.Enums.PolicyStatus.Lapsed,
            Models.Enums.PolicyStatus.Claimed
        };
        var livePolicies = await _unitOfWork.Policies.FindAsync(p =>
            p.ProductId == productId && livePolicyStatuses.Contains(p.Status));

        var proposalCount = activeProposals.Count();
        var policyCount = livePolicies.Count();
        if (proposalCount > 0 || policyCount > 0)
        {
            throw new ConflictException(
                $"Cannot deactivate this product because it has {proposalCount} active proposal(s) and {policyCount} live policy record(s). Resolve or migrate them before deactivation.");
        }
    }
}
