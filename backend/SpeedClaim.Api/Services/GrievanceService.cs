using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Grievances;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class GrievanceService : IGrievanceService
{
    private readonly IUnitOfWork _unitOfWork;

    public GrievanceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GrievanceDto> RaiseGrievanceAsync(Guid customerId, RaiseGrievanceRequest request)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
        if (customer == null) throw new KeyNotFoundException("Customer not found.");

        if (request.PolicyId.HasValue)
        {
            var policy = await _unitOfWork.Policies.GetByIdAsync(request.PolicyId.Value);
            if (policy == null || policy.CustomerId != customerId)
                throw new InvalidOperationException("Invalid policy.");
        }

        if (request.ClaimId.HasValue)
        {
            var claim = await _unitOfWork.Claims.GetByIdAsync(request.ClaimId.Value);
            if (claim == null || claim.CustomerId != customerId)
                throw new InvalidOperationException("Invalid claim.");
        }

        var grievance = new Grievance
        {
            Id = Guid.NewGuid(),
            GrievanceNumber = $"GRV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            CustomerId = customerId,
            PolicyId = request.PolicyId,
            ClaimId = request.ClaimId,
            Category = request.Category,
            Description = request.Description,
            Status = GrievanceStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Grievances.AddAsync(grievance);
        await _unitOfWork.CompleteAsync();

        return MapToDto(grievance);
    }

    public async Task<IEnumerable<GrievanceDto>> GetMyGrievancesAsync(Guid customerId)
    {
        var grievances = await _unitOfWork.Grievances.FindAsync(g => g.CustomerId == customerId);
        return grievances.Select(MapToDto);
    }

    public async Task<IEnumerable<GrievanceDto>> GetAllGrievancesAsync()
    {
        var grievances = await _unitOfWork.Grievances.GetAllAsync();
        return grievances.Select(MapToDto);
    }

    public async Task<GrievanceDto> GetGrievanceByIdAsync(Guid id)
    {
        var grievance = await _unitOfWork.Grievances.GetByIdAsync(id);
        if (grievance == null) throw new KeyNotFoundException("Grievance not found.");
        return MapToDto(grievance);
    }

    public async Task AssignGrievanceAsync(Guid grievanceId, Guid officerId)
    {
        var grievance = await _unitOfWork.Grievances.GetByIdAsync(grievanceId);
        if (grievance == null) throw new KeyNotFoundException("Grievance not found.");

        grievance.AssignedToId = officerId;
        grievance.Status = GrievanceStatus.InProgress;
        grievance.UpdatedAt = DateTimeOffset.UtcNow;

        _unitOfWork.Grievances.Update(grievance);
        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateGrievanceStatusAsync(Guid grievanceId, UpdateGrievanceStatusRequest request)
    {
        var grievance = await _unitOfWork.Grievances.GetByIdAsync(grievanceId);
        if (grievance == null) throw new KeyNotFoundException("Grievance not found.");

        grievance.Status = request.Status;
        
        if (request.ResolutionNotes != null)
        {
            grievance.ResolutionNotes = request.ResolutionNotes;
        }

        if (request.Status == GrievanceStatus.Resolved || request.Status == GrievanceStatus.Closed)
        {
            grievance.ResolvedAt = DateTimeOffset.UtcNow;
        }

        grievance.UpdatedAt = DateTimeOffset.UtcNow;

        _unitOfWork.Grievances.Update(grievance);
        await _unitOfWork.CompleteAsync();
    }

    private static GrievanceDto MapToDto(Grievance g)
    {
        return new GrievanceDto(
            g.Id,
            g.GrievanceNumber,
            g.CustomerId,
            g.PolicyId,
            g.ClaimId,
            g.Category.ToString(),
            g.Description,
            g.Status.ToString(),
            g.AssignedToId,
            g.ResolutionNotes,
            g.ResolvedAt,
            g.CreatedAt
        );
    }
}
