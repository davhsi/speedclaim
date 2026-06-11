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

    /// <summary>Raise a new grievance against a policy or claim</summary>
    /// <remarks>PolicyId and ClaimId are optional. Grievance is opened in Open status.</remarks>
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType(typeof(GrievanceDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RaiseGrievance([FromBody] RaiseGrievanceRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _grievanceService.RaiseGrievanceAsync(customerId, request);
        return Ok(result);
    }

    /// <summary>Get all grievances raised by the authenticated customer</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<GrievanceDto>), 200)]
    public async Task<IActionResult> GetMyGrievances()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _grievanceService.GetMyGrievancesAsync(customerId);
        return Ok(result);
    }

    /// <summary>Get all grievances across all customers</summary>
    [Authorize(Roles = "Admin,ClaimsOfficer")]
    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<GrievanceDto>), 200)]
    public async Task<IActionResult> GetAllGrievances()
    {
        var result = await _grievanceService.GetAllGrievancesAsync();
        return Ok(result);
    }

    /// <summary>Get a grievance by ID</summary>
    /// <param name="id">Grievance ID</param>
    [Authorize(Roles = "Admin,ClaimsOfficer")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GrievanceDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetGrievanceById(Guid id)
    {
        var result = await _grievanceService.GetGrievanceByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Assign a grievance to an officer for resolution</summary>
    /// <param name="id">Grievance ID</param>
    [Authorize(Roles = "Admin,ClaimsOfficer")]
    [HttpPut("{id}/assign")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignGrievance(Guid id, [FromBody] AssignGrievanceRequest request)
    {
        await _grievanceService.AssignGrievanceAsync(id, request.AssignedToId);
        return Ok();
    }

    /// <summary>Update the status of a grievance and optionally add resolution notes</summary>
    /// <param name="id">Grievance ID</param>
    [Authorize(Roles = "Admin,ClaimsOfficer")]
    [HttpPut("{id}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateGrievanceStatus(Guid id, [FromBody] UpdateGrievanceStatusRequest request)
    {
        await _grievanceService.UpdateGrievanceStatusAsync(id, request);
        return Ok();
    }
}
