using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.Grievances;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Services;

public class GrievanceService : IGrievanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public GrievanceService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    private async Task<Models.Customer> ResolveCustomerAsync(Guid userId)
    {
        var customer = await _unitOfWork.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) throw new NotFoundException("Customer not found.");
        return customer;
    }

    public async Task<GrievanceDto> RaiseGrievanceAsync(Guid customerId, RaiseGrievanceRequest request)
    {
        var customer = await ResolveCustomerAsync(customerId);
        var customerRecordId = customer.Id;

        if (request.PolicyId.HasValue)
        {
            var policy = await _unitOfWork.Policies.GetByIdAsync(request.PolicyId.Value);
            if (policy == null || policy.CustomerId != customerRecordId)
                throw new ValidationException("Invalid policy.");
        }

        if (request.ClaimId.HasValue)
        {
            var claim = await _unitOfWork.Claims.GetByIdAsync(request.ClaimId.Value);
            if (claim == null || claim.CustomerId != customerRecordId)
                throw new ValidationException("Invalid claim.");
        }

        var grievance = new Grievance
        {
            Id = Guid.NewGuid(),
            GrievanceNumber = $"GRV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
            CustomerId = customerRecordId,
            PolicyId = request.PolicyId,
            ClaimId = request.ClaimId,
            Category = request.Category,
            Description = request.Description,
            Status = GrievanceStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Grievances.AddAsync(grievance);
        await _unitOfWork.CompleteAsync();

        var user = await _unitOfWork.Users.GetByIdAsync(customerId);
        if (user != null)
        {
            await _emailService.SendTemplatedEmailAsync("GrievanceFiled", new Dictionary<string, string>
            {
                ["firstName"]       = WebUtility.HtmlEncode(user.FirstName),
                ["grievanceNumber"] = WebUtility.HtmlEncode(grievance.GrievanceNumber)
            }, user.Email);
        }

        return MapToDto(grievance);
    }

    public async Task<IEnumerable<GrievanceDto>> GetMyGrievancesAsync(Guid customerId)
    {
        var customer = await ResolveCustomerAsync(customerId);
        var grievances = await _unitOfWork.Grievances.FindAsync(g => g.CustomerId == customer.Id);
        return grievances.Select(MapToDto);
    }

    public async Task<PagedResponse<GrievanceDto>> GetAllGrievancesAsync(int page, int pageSize)
    {
        var (items, total) = await _unitOfWork.Grievances.GetPagedAsync(page, pageSize);
        return new PagedResponse<GrievanceDto>(items.Select(MapToDto), page, pageSize, total);
    }

    public async Task<GrievanceDto> GetGrievanceByIdAsync(Guid id, Guid? requestingCustomerId = null)
    {
        var grievance = await _unitOfWork.Grievances.GetByIdAsync(id);
        if (grievance == null) throw new NotFoundException("Grievance not found.");
        if (requestingCustomerId.HasValue)
        {
            var customer = await ResolveCustomerAsync(requestingCustomerId.Value);
            if (grievance.CustomerId != customer.Id)
                throw new ForbiddenException("You do not have access to this grievance.");
        }
        return MapToDto(grievance);
    }

    public async Task AssignGrievanceAsync(Guid grievanceId, Guid officerId)
    {
        var grievance = await _unitOfWork.Grievances.GetByIdAsync(grievanceId);
        if (grievance == null) throw new NotFoundException("Grievance not found.");
        if (IsTerminal(grievance.Status))
            throw new ValidationException("Resolved or closed grievances cannot be reassigned.");

        grievance.AssignedToId = officerId;
        grievance.Status = GrievanceStatus.InProgress;
        grievance.UpdatedAt = DateTimeOffset.UtcNow;

        _unitOfWork.Grievances.Update(grievance);
        await _unitOfWork.CompleteAsync();
    }

    public async Task UpdateGrievanceStatusAsync(Guid grievanceId, UpdateGrievanceStatusRequest request)
    {
        var grievance = await _unitOfWork.Grievances.GetByIdAsync(grievanceId);
        if (grievance == null) throw new NotFoundException("Grievance not found.");
        if (IsTerminal(grievance.Status))
            throw new ValidationException("Resolved or closed grievances cannot be updated.");

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

        if (request.Status == GrievanceStatus.Resolved || request.Status == GrievanceStatus.Escalated)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(grievance.CustomerId);
            if (customer != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(customer.UserId);
                if (user != null)
                {
                    if (request.Status == GrievanceStatus.Resolved)
                        await _emailService.SendTemplatedEmailAsync("GrievanceResolved", new Dictionary<string, string>
                        {
                            ["firstName"]       = WebUtility.HtmlEncode(user.FirstName),
                            ["grievanceNumber"] = WebUtility.HtmlEncode(grievance.GrievanceNumber),
                            ["resolutionNotes"] = WebUtility.HtmlEncode(grievance.ResolutionNotes ?? "Your grievance has been resolved.")
                        }, user.Email);
                    else
                        await _emailService.SendTemplatedEmailAsync("GrievanceEscalated", new Dictionary<string, string>
                        {
                            ["firstName"]       = WebUtility.HtmlEncode(user.FirstName),
                            ["grievanceNumber"] = WebUtility.HtmlEncode(grievance.GrievanceNumber)
                        }, user.Email);
                }
            }
        }
    }

    private static bool IsTerminal(GrievanceStatus status)
    {
        return status == GrievanceStatus.Resolved || status == GrievanceStatus.Closed;
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
