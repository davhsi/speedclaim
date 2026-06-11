using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Dtos.Policies;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize]
public class PolicyController : BaseApiController
{
    private readonly IPolicyService _policyService;

    public PolicyController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyPolicies()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _policyService.GetMyPoliciesAsync(customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadPolicy(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var bytes = await _policyService.DownloadPolicyDocumentAsync(id, customerId);
        return File(bytes, "text/plain", $"Policy_{id}.txt");
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("{id}/endorsements")]
    public async Task<IActionResult> GetPolicyEndorsements(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _policyService.GetPolicyEndorsementsAsync(id, customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("{id}/endorsements")]
    public async Task<IActionResult> RequestEndorsement(Guid id, [FromBody] RequestEndorsementRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        await _policyService.RequestEndorsementAsync(id, customerId, request);
        return Ok();
    }

    [Authorize(Roles = "Agent")]
    [HttpGet("assigned")]
    public async Task<IActionResult> GetAssignedCustomerPolicies()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var agentId)) return Unauthorized();
        var result = await _policyService.GetAssignedCustomerPoliciesAsync(agentId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer,Underwriter,Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPolicy(Guid id)
    {
        Guid? customerId = null;
        if (User.IsInRole("Customer"))
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var cid)) return Unauthorized();
            customerId = cid;
        }
        var result = await _policyService.GetByIdAsync(id, customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("{id}/nominees")]
    public async Task<IActionResult> GetNominees(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _policyService.GetNomineesAsync(id, customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelPolicy(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        await _policyService.CancelPolicyAsync(id, customerId);
        return Ok();
    }

    [Authorize(Roles = "Underwriter,Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllPolicies()
    {
        var result = await _policyService.GetAllPoliciesAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Underwriter,Admin,Customer")]
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetPolicyHistory(Guid id)
    {
        Guid? customerId = null;
        if (User.IsInRole("Customer"))
        {
            if (!Guid.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var cid))
                return Unauthorized();
            customerId = cid;
        }
        var result = await _policyService.GetPolicyHistoryAsync(id, customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Underwriter")]
    [HttpGet("endorsements/pending")]
    public async Task<IActionResult> GetPendingEndorsements()
    {
        var result = await _policyService.GetPendingEndorsementsAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Underwriter")]
    [HttpPut("endorsements/{endorsementId}/review")]
    public async Task<IActionResult> ApproveRejectEndorsement(Guid endorsementId, [FromBody] ApproveRejectEndorsementRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var underwriterId)) return Unauthorized();
        await _policyService.ApproveRejectEndorsementAsync(endorsementId, request.IsApproved, request.Reason, underwriterId);
        return Ok();
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("nominees/{nomineeId}")]
    public async Task<IActionResult> UpdateNominee(Guid nomineeId, [FromBody] UpdateNomineeRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        await _policyService.UpdateNomineeAsync(nomineeId, customerId, request);
        return Ok();
    }
}
