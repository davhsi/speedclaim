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
        return CreatedAtAction(nameof(GetById), new { id = policy.Id }, policy);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var policy = await _policyService.GetPolicyByIdAsync(id);
        return Ok(policy);
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        if (currentUserIdStr != userId.ToString() && role == "CUSTOMER")
        {
            return Forbid();
        }

        var policies = await _policyService.GetPoliciesByUserAsync(userId);
        return Ok(policies);
    }
}
