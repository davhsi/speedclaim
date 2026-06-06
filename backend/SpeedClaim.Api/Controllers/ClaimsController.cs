using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Claims;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Controllers;

using Asp.Versioning;

[ApiVersion("1.0")]
public class ClaimsController : BaseApiController
{
    private readonly IClaimService _claimService;

    public ClaimsController(IClaimService claimService)
    {
        _claimService = claimService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllClaims([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID not found in token.");

        var response = await _claimService.GetAllClaimsAsync(userId, pageNumber, pageSize);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> SubmitClaim([FromForm] SubmitClaimRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID not found in token.");

        var response = await _claimService.SubmitClaimAsync(userId, request);
        return Ok(response);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Agent, Admin")]
    public async Task<IActionResult> UpdateClaimStatus(Guid id, [FromBody] UpdateClaimStatusRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var actorId))
            return Unauthorized("User ID not found in token.");

        try
        {
            var response = await _claimService.UpdateClaimStatusAsync(id, actorId, request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/checklist")]
    [Authorize]
    public async Task<IActionResult> GetClaimChecklist(Guid id)
    {
        var response = await _claimService.GetClaimChecklistAsync(id);
        return Ok(response);
    }
}
