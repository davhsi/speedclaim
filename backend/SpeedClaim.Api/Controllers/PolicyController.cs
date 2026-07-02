using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Filters;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Services;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize]
public class PoliciesController : BaseApiController
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    #region Customer Endpoints

    /// <summary>Get all policies belonging to the authenticated customer</summary>
    /// <param name="status">Optional filter by policy status (e.g. Active, Lapsed, Cancelled, Expired)</param>
    /// <param name="type">Optional filter by policy type (e.g. Individual, FamilyFloater)</param>
    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<PolicyDto>), 200)]
    public async Task<IActionResult> GetMyPolicies([FromQuery] string? status = null, [FromQuery] string? type = null)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        PolicyStatus? statusFilter = Enum.TryParse<PolicyStatus>(status, true, out var s) ? s : null;
        PolicyType? typeFilter = Enum.TryParse<PolicyType>(type, true, out var t) ? t : null;
        var result = await _policyService.GetMyPoliciesAsync(customerId, statusFilter, typeFilter);
        return Ok(result);
    }

    /// <summary>Download a PDF policy certificate for a specific policy</summary>
    /// <param name="id">Policy ID</param>
    [Authorize(Roles = "Customer")]
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DownloadPolicy(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var bytes = await _policyService.DownloadPolicyDocumentAsync(id, customerId);
        return File(bytes, PolicyDocumentGenerator.ContentType, $"Policy_{id}.pdf");
    }

    /// <summary>Get all endorsement requests raised against a policy</summary>
    /// <param name="id">Policy ID</param>
    [Authorize(Roles = "Customer")]
    [HttpGet("{id}/endorsements")]
    [ProducesResponseType(typeof(IEnumerable<EndorsementDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPolicyEndorsements(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _policyService.GetPolicyEndorsementsAsync(id, customerId);
        return Ok(result);
    }

    /// <summary>Request a policy endorsement (amendment) such as address change or sum assured update</summary>
    /// <param name="id">Policy ID</param>
    [Authorize(Roles = "Customer")]
    [HttpPost("{id}/endorsements")]
    [Idempotent]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RequestEndorsement(Guid id, [FromBody] RequestEndorsementRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        await _policyService.RequestEndorsementAsync(id, customerId, request);
        return Ok(new { message = "Endorsement request submitted and is pending underwriter review." });
    }

    /// <summary>Get all nominees linked to a policy</summary>
    /// <param name="id">Policy ID</param>
    [Authorize(Roles = "Customer")]
    [HttpGet("{id}/nominees")]
    [ProducesResponseType(typeof(IEnumerable<PolicyNomineeDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetNominees(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _policyService.GetNomineesAsync(id, customerId);
        return Ok(result);
    }

    /// <summary>Update nominee details (name, relationship, share percentage) on a policy</summary>
    /// <param name="nomineeId">Nominee ID</param>
    [Authorize(Roles = "Customer")]
    [HttpPatch("nominees/{nomineeId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateNominee(Guid nomineeId, [FromBody] UpdateNomineeRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        await _policyService.UpdateNomineeAsync(nomineeId, customerId, request);
        return Ok(new { message = "Nominee details updated successfully." });
    }

    /// <summary>Cancel an Active or Pending policy</summary>
    /// <param name="id">Policy ID</param>
    [Authorize(Roles = "Customer")]
    [HttpPut("{id}/cancel")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> CancelPolicy(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        await _policyService.CancelPolicyAsync(id, customerId);
        return Ok(new { message = "Policy cancelled successfully." });
    }

    #endregion

    #region Agent Endpoints

    /// <summary>Agent — get all policies belonging to customers assigned to the authenticated agent</summary>
    [Authorize(Roles = "Agent")]
    [HttpGet("assigned")]
    [ProducesResponseType(typeof(IEnumerable<PolicyDto>), 200)]
    public async Task<IActionResult> GetAssignedCustomerPolicies()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var agentId)) return Unauthorized();
        var result = await _policyService.GetAssignedCustomerPoliciesAsync(agentId);
        return Ok(result);
    }

    #endregion

    #region Shared Endpoints

    /// <summary>Get a policy by ID. Customers can only access their own policies</summary>
    /// <param name="id">Policy ID</param>
    [Authorize(Roles = "Customer,Underwriter,Admin")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PolicyDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
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

    /// <summary>Get the status change history for a policy. Customers can only view their own policy history</summary>
    /// <param name="id">Policy ID</param>
    [Authorize(Roles = "Underwriter,Admin,Customer")]
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(IEnumerable<PolicyStatusHistoryDto>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
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

    #endregion

    #region Underwriter Endpoints

    /// <summary>Get all policies across all customers</summary>
    /// <param name="page">Page number (default 1)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="status">Optional filter by policy status (e.g. Active, Lapsed, Cancelled, Expired)</param>
    /// <param name="type">Optional filter by policy type (e.g. Individual, FamilyFloater)</param>
    [Authorize(Roles = "Underwriter,Admin")]
    [HttpGet("all")]
    [ProducesResponseType(typeof(PagedResponse<PolicyDto>), 200)]
    public async Task<IActionResult> GetAllPolicies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        PolicyStatus? statusFilter = Enum.TryParse<PolicyStatus>(status, true, out var s) ? s : null;
        PolicyType? typeFilter = Enum.TryParse<PolicyType>(type, true, out var t) ? t : null;
        var result = await _policyService.GetAllPoliciesAsync(page, pageSize, statusFilter, typeFilter);
        return Ok(result);
    }

    /// <summary>Underwriter — get all endorsement requests awaiting review</summary>
    [Authorize(Roles = "Underwriter")]
    [HttpGet("endorsements/pending")]
    [ProducesResponseType(typeof(PagedResponse<EndorsementDto>), 200)]
    public async Task<IActionResult> GetPendingEndorsements([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _policyService.GetPendingEndorsementsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>Underwriter — approve or reject an endorsement request</summary>
    /// <param name="endorsementId">Endorsement ID</param>
    [Authorize(Roles = "Underwriter")]
    [HttpPut("endorsements/{endorsementId}/review")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> ApproveRejectEndorsement(Guid endorsementId, [FromBody] ApproveRejectEndorsementRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var underwriterId)) return Unauthorized();
        await _policyService.ApproveRejectEndorsementAsync(endorsementId, request.IsApproved, request.Reason, underwriterId);
        return Ok(new { message = request.IsApproved ? "Endorsement approved and applied to the policy." : "Endorsement rejected." });
    }

    #endregion
}
