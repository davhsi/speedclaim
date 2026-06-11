using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Dtos.Claims;
using SpeedClaim.Api.Dtos.Common;
using System.Security.Claims;
using SpeedClaim.Api.Models.Enums;

namespace SpeedClaim.Api.Controllers;

[Authorize]
public class ClaimsController : BaseApiController
{
    private readonly IClaimService _claimService;

    public ClaimsController(IClaimService claimService)
    {
        _claimService = claimService;
    }

    #region Customer Endpoints

    /// <summary>Intimate (register) a new claim against an active policy</summary>
    /// <remarks>Policy must be in Active status. Claim is created in Intimated state.</remarks>
    [Authorize(Roles = "Customer")]
    [HttpPost("intimate")]
    [ProducesResponseType(typeof(ClaimDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> IntimateClaim([FromBody] IntimateClaimRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _claimService.IntimateClaimAsync(customerId, request);
        return Ok(result);
    }

    /// <summary>Upload a supporting document for a claim</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "Customer")]
    [HttpPost("{id}/upload")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UploadClaimDocument(Guid id, [FromForm] UploadDocumentRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _claimService.UploadClaimDocumentAsync(id, customerId, request.DocumentType, request.File);
        return Ok(new { FilePath = result });
    }

    /// <summary>Get all claims belonging to the authenticated customer</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<ClaimDto>), 200)]
    public async Task<IActionResult> GetMyClaims()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _claimService.GetMyClaimsAsync(customerId);
        return Ok(result);
    }

    /// <summary>Get the status history timeline for a specific claim</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "Customer")]
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(IEnumerable<ClaimStatusHistoryDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMyClaimHistory(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _claimService.GetClaimHistoryAsync(id, customerId);
        return Ok(result);
    }

    /// <summary>Get a claim by ID. Customers can only access their own claims</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "Customer,ClaimsOfficer,Admin")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ClaimDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetClaimById(Guid id)
    {
        Guid? customerId = null;
        if (User.IsInRole("Customer"))
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var cid)) return Unauthorized();
            customerId = cid;
        }
        var result = await _claimService.GetClaimByIdAsync(id, customerId);
        return Ok(result);
    }

    #endregion

    #region Claims Officer Endpoints

    /// <summary>Get all claims across all customers</summary>
    [Authorize(Roles = "ClaimsOfficer,Admin")]
    [HttpGet("all")]
    [ProducesResponseType(typeof(PagedResponse<ClaimDto>), 200)]
    public async Task<IActionResult> GetAllClaims([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _claimService.GetAllClaimsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>Self-assign a claim to the authenticated claims officer</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/assign")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignClaim(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.AssignClaimAsync(id, officerId);
        return Ok();
    }

    /// <summary>Update the status of a claim manually</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateClaimStatusRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        if (!Enum.TryParse<ClaimStatus>(request.Status, true, out var claimStatus)) return BadRequest("Invalid Status");
        await _claimService.UpdateClaimStatusAsync(id, claimStatus, officerId, request.Remarks);
        return Ok();
    }

    /// <summary>Approve or reject a claim and optionally set the approved payout amount</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/approve")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApproveClaim(Guid id, [FromBody] ApproveRejectClaimRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.ApproveOrRejectClaimAsync(id, request, officerId);
        return Ok();
    }

    /// <summary>Mark an approved claim as financially settled</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/settle")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> MarkSettled(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.MarkClaimAsSettledAsync(id, officerId);
        return Ok();
    }

    /// <summary>Assign a surveyor to a motor or property claim</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/assign-surveyor")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> AssignSurveyor(Guid id, [FromBody] AssignSurveyorRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.AssignSurveyorAsync(id, request.SurveyorId, officerId, request.Notes);
        return Ok();
    }

    /// <summary>Request additional documents from the customer for a claim</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPost("{id}/request-docs")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RequestDocuments(Guid id, [FromBody] string details)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.RequestAdditionalDocumentsAsync(id, details, officerId);
        return Ok();
    }

    /// <summary>Approve cashless pre-authorisation for a cashless claim</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/approve-preauth")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> ApprovePreAuth(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.ApproveCashlessPreAuthAsync(id, officerId);
        return Ok();
    }

    #endregion

    #region Surveyor Endpoints

    /// <summary>Get all motor/property claims assigned to the authenticated surveyor</summary>
    [Authorize(Roles = "Surveyor")]
    [HttpGet("surveyor/assigned")]
    [ProducesResponseType(typeof(IEnumerable<ClaimDto>), 200)]
    public async Task<IActionResult> GetAssignedMotorClaims()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var surveyorId)) return Unauthorized();
        var result = await _claimService.GetAssignedMotorClaimsAsync(surveyorId);
        return Ok(result);
    }

    /// <summary>Upload a survey inspection report for a motor or property claim</summary>
    /// <param name="id">Claim ID</param>
    [Authorize(Roles = "Surveyor,ClaimsOfficer")]
    [HttpPost("{id}/survey-report")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UploadSurveyReport(Guid id, [FromForm] SubmitSurveyReportRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await _claimService.SubmitSurveyReportAsync(id, userId, request);
        return Ok();
    }

    #endregion
}
