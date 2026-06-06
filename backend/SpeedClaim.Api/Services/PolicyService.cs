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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PolicyService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PolicyDto> IssuePolicyAsync(CreatePolicyRequest request, Guid? agentUserId = null)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
        if (product == null) throw new ArgumentException("Invalid product ID.");

        if (product.Domain != request.Domain.ToUpper())
            throw new ArgumentException("Product domain does not match request domain.");

        Guid? agentId = null;
        if (agentUserId.HasValue)
        {
            var agent = await _unitOfWork.Agents.SingleOrDefaultAsync(a => a.UserId == agentUserId.Value);
            if (agent != null) agentId = agent.Id;
        }

        Policy policy;
        var policyId = Guid.NewGuid();
        var policyNumber = $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        
        if (request.Domain.ToUpper() == "HEALTH")
        {
            policy = new HealthPolicy
            {
                CoversDental = request.HealthDetail?.CoversDental ?? false,
                Deductible = request.HealthDetail?.Deductible ?? 0,
                NetworkType = request.HealthDetail?.NetworkType?.ToUpper() ?? "TPA"
            };
        }
        else if (request.Domain.ToUpper() == "VEHICLE")
        {
            policy = new VehiclePolicy
            {
                VehicleNumber = request.VehicleDetail?.VehicleNumber ?? "",
                Make = request.VehicleDetail?.Make ?? "",
                Model = request.VehicleDetail?.Model ?? "",
                ManufactureYear = request.VehicleDetail?.ManufactureYear ?? DateTime.UtcNow.Year,
                InsuredDeclaredValue = request.VehicleDetail?.InsuredDeclaredValue ?? 0,
                IsComprehensive = request.VehicleDetail?.IsComprehensive ?? false
            };
        }
        else if (request.Domain.ToUpper() == "LIFE")
        {
            policy = new LifePolicy
            {
                NomineeName = request.LifeDetail?.NomineeName ?? "",
                NomineeRelation = request.LifeDetail?.NomineeRelation ?? "",
                NomineePhone = request.LifeDetail?.NomineePhone ?? "",
                HasAccidentalRider = request.LifeDetail?.HasAccidentalRider ?? false
            };
        }
        else
        {
            throw new ArgumentException("Unknown domain");
        }

        policy.Id = policyId;
        policy.PolicyNumber = policyNumber;
        policy.UserId = request.UserId;
        policy.ProductId = request.ProductId;
        policy.AgentId = agentId;
        policy.Status = "PENDING";
        policy.PaymentFrequency = request.PaymentFrequency.ToUpper();
        policy.PremiumAmount = request.PremiumAmount;
        policy.CoverageAmount = request.CoverageAmount;
        policy.Currency = "INR";
        policy.StartDate = request.StartDate.ToUniversalTime();
        policy.EndDate = request.EndDate.ToUniversalTime();
        policy.Domain = request.Domain.ToUpper();
        
        // Generate Premium Schedules
        int installments = policy.PaymentFrequency switch
        {
            "MONTHLY" => 12,
            "QUARTERLY" => 4,
            "ANNUAL" => 1,
            _ => 1
        };
        
        var amountPerInstallment = policy.PremiumAmount / installments;
        for (int i = 0; i < installments; i++)
        {
            var dueDate = policy.PaymentFrequency switch
            {
                "MONTHLY" => policy.StartDate.AddMonths(i),
                "QUARTERLY" => policy.StartDate.AddMonths(i * 3),
                "ANNUAL" => policy.StartDate.AddYears(i),
                _ => policy.StartDate
            };

            policy.PremiumSchedules.Add(new PremiumSchedule
            {
                Id = Guid.NewGuid(),
                PolicyId = policyId,
                InstallmentNumber = i + 1,
                AmountDue = amountPerInstallment,
                DueDate = dueDate,
                Status = "PENDING"
            });
        }

        await _unitOfWork.Policies.AddAsync(policy);
        await _unitOfWork.CompleteAsync();

        return await GetPolicyByIdAsync(policy.Id);
    }

    public async Task<PolicyDto> GetPolicyByIdAsync(Guid policyId)
    {
        var policy = await _unitOfWork.Policies.GetByIdAsync(policyId);

        if (policy == null) throw new KeyNotFoundException("Policy not found.");

        return _mapper.Map<PolicyDto>(policy);
    }

    public async Task<SpeedClaim.Api.Dtos.Common.PagedResponse<PolicyDto>> GetPoliciesByUserAsync(Guid userId, int pageNumber, int pageSize)
    {
        var (policies, totalCount) = await _unitOfWork.Policies.GetPagedAsync(
            pageNumber, 
            pageSize, 
            p => p.UserId == userId, 
            query => query.Include(p => p.Product));

        var policyDtos = _mapper.Map<IEnumerable<PolicyDto>>(policies);
        return new SpeedClaim.Api.Dtos.Common.PagedResponse<PolicyDto>(policyDtos, pageNumber, pageSize, totalCount);
    }
}
