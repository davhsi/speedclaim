using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Interfaces;

using Asp.Versioning;

namespace SpeedClaim.Api.Controllers;

[ApiVersion("1.0")]
[Authorize]
public class PolicyController : BaseApiController
{
    private readonly IPolicyService _policyService;

    public PolicyController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserPolicies([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID not found in token.");

        var policies = await _policyService.GetPoliciesByUserAsync(userId, pageNumber, pageSize);
        return Ok(policies);
    }

    [HttpPost]
    public async Task<IActionResult> IssuePolicy([FromBody] CreatePolicyRequest request)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        Guid? agentUserId = null;

        if (role == "AGENT")
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                agentUserId = userId;
            }
        }

        var policy = await _policyService.IssuePolicyAsync(request, agentUserId);
        return Created("", policy);
    }
    
    
}
