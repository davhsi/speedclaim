using System;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Financial;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Controllers;

[ApiVersion("1.0")]
public class FinancialsController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public FinancialsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("policies/{policyId}/premium-schedules")]
    [Authorize]
    public async Task<IActionResult> GetPremiumSchedules(Guid policyId)
    {
        // TODO: Validate user is policy owner or admin
        
        var schedules = await _unitOfWork.PremiumSchedules.FindAsync(s => s.PolicyId == policyId);
        
        var dtos = schedules.Select(s => new PremiumScheduleDto
        {
            Id = s.Id,
            PolicyId = s.PolicyId,
            InstallmentNumber = s.InstallmentNumber,
            AmountDue = s.AmountDue,
            DueDate = s.DueDate,
            Status = s.Status,
            PaymentId = s.PaymentId
        }).OrderBy(s => s.InstallmentNumber);

        return Ok(dtos);
    }
}
