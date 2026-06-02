using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public class PolicyService : IPolicyService
{
    private readonly SpeedClaimDbContext _context;
    private readonly IMapper _mapper;

    public PolicyService(SpeedClaimDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PolicyDto> IssuePolicyAsync(CreatePolicyRequest request, Guid? agentUserId = null)
    {
        var product = await _context.InsuranceProducts.FindAsync(request.ProductId);
        if (product == null) throw new ArgumentException("Invalid product ID.");

        if (product.Domain != request.Domain.ToUpper())
            throw new ArgumentException("Product domain does not match request domain.");

        Guid? agentId = null;
        if (agentUserId.HasValue)
        {
            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.UserId == agentUserId.Value);
            if (agent != null) agentId = agent.Id;
        }

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            PolicyNumber = $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            UserId = request.UserId,
            ProductId = request.ProductId,
            AgentId = agentId,
            Status = "ACTIVE",
            PaymentFrequency = request.PaymentFrequency.ToUpper(),
            PremiumAmount = request.PremiumAmount,
            CoverageAmount = request.CoverageAmount,
            Currency = "INR",
            StartDate = request.StartDate.ToUniversalTime(),
            EndDate = request.EndDate.ToUniversalTime(),
            Domain = request.Domain.ToUpper()
        };

        if (policy.Domain == "HEALTH" && request.HealthDetail != null)
        {
            policy.HealthDetail = new PolicyHealthDetail
            {
                PolicyId = policy.Id,
                CoversDental = request.HealthDetail.CoversDental,
                Deductible = request.HealthDetail.Deductible,
                NetworkType = request.HealthDetail.NetworkType.ToUpper()
            };
        }
        else if (policy.Domain == "VEHICLE" && request.VehicleDetail != null)
        {
            policy.VehicleDetail = new PolicyVehicleDetail
            {
                PolicyId = policy.Id,
                VehicleNumber = request.VehicleDetail.VehicleNumber,
                Make = request.VehicleDetail.Make,
                Model = request.VehicleDetail.Model,
                ManufactureYear = request.VehicleDetail.ManufactureYear,
                InsuredDeclaredValue = request.VehicleDetail.InsuredDeclaredValue,
                IsComprehensive = request.VehicleDetail.IsComprehensive
            };
        }
        else if (policy.Domain == "LIFE" && request.LifeDetail != null)
        {
            policy.LifeDetail = new PolicyLifeDetail
            {
                PolicyId = policy.Id,
                NomineeName = request.LifeDetail.NomineeName,
                NomineeRelation = request.LifeDetail.NomineeRelation,
                NomineePhone = request.LifeDetail.NomineePhone,
                HasAccidentalRider = request.LifeDetail.HasAccidentalRider
            };
        }

        _context.Policies.Add(policy);
        await _context.SaveChangesAsync();

        return await GetPolicyByIdAsync(policy.Id);
    }

    public async Task<PolicyDto> GetPolicyByIdAsync(Guid policyId)
    {
        var policy = await _context.Policies
            .Include(p => p.HealthDetail)
            .Include(p => p.VehicleDetail)
            .Include(p => p.LifeDetail)
            .FirstOrDefaultAsync(p => p.Id == policyId);

        if (policy == null) throw new KeyNotFoundException("Policy not found.");

        return _mapper.Map<PolicyDto>(policy);
    }

    public async Task<IEnumerable<PolicyDto>> GetPoliciesByUserAsync(Guid userId)
    {
        var policies = await _context.Policies
            .Include(p => p.HealthDetail)
            .Include(p => p.VehicleDetail)
            .Include(p => p.LifeDetail)
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return _mapper.Map<IEnumerable<PolicyDto>>(policies);
    }
}
