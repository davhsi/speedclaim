using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Dtos.Grievances;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize]
public class GrievancesController : BaseApiController
{
    private readonly IGrievanceService _grievanceService;

    public GrievancesController(IGrievanceService grievanceService)
    {
        _grievanceService = grievanceService;
    }

    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> RaiseGrievance([FromBody] RaiseGrievanceRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _grievanceService.RaiseGrievanceAsync(customerId, request);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyGrievances()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _grievanceService.GetMyGrievancesAsync(customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,ClaimsOfficer")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllGrievances()
    {
        var result = await _grievanceService.GetAllGrievancesAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Admin,ClaimsOfficer")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetGrievanceById(Guid id)
    {
        var result = await _grievanceService.GetGrievanceByIdAsync(id);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,ClaimsOfficer")]
    [HttpPut("{id}/assign")]
    public async Task<IActionResult> AssignGrievance(Guid id, [FromBody] AssignGrievanceRequest request)
    {
        await _grievanceService.AssignGrievanceAsync(id, request.AssignedToId);
        return Ok();
    }

    [Authorize(Roles = "Admin,ClaimsOfficer")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateGrievanceStatus(Guid id, [FromBody] UpdateGrievanceStatusRequest request)
    {
        await _grievanceService.UpdateGrievanceStatusAsync(id, request);
        return Ok();
    }
}
