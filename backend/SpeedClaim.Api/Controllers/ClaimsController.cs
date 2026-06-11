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

    // --- Customer Endpoints ---
    [Authorize(Roles = "Customer")]
    [HttpPost("intimate")]
    public async Task<IActionResult> IntimateClaim([FromBody] IntimateClaimRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _claimService.IntimateClaimAsync(customerId, request);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("{id}/upload")]
    public async Task<IActionResult> UploadClaimDocument(Guid id, [FromForm] UploadDocumentRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _claimService.UploadClaimDocumentAsync(id, customerId, request.DocumentType, request.File);
        return Ok(new { FilePath = result });
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyClaims()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _claimService.GetMyClaimsAsync(customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetMyClaimHistory(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var customerId)) return Unauthorized();
        var result = await _claimService.GetClaimHistoryAsync(id, customerId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer,ClaimsOfficer,Admin")]
    [HttpGet("{id}")]
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

    // --- Claims Officer Endpoints ---
    [Authorize(Roles = "ClaimsOfficer,Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllClaims()
    {
        var result = await _claimService.GetAllClaimsAsync();
        return Ok(result);
    }

    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/assign")]
    public async Task<IActionResult> AssignClaim(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.AssignClaimAsync(id, officerId);
        return Ok();
    }

    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateClaimStatusRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        // The DTO UpdateClaimStatusRequest was from previous implementation, I'll map its string status to Enum
        if (!Enum.TryParse<ClaimStatus>(request.Status, true, out var claimStatus)) return BadRequest("Invalid Status");
        await _claimService.UpdateClaimStatusAsync(id, claimStatus, officerId, request.Remarks);
        return Ok();
    }

    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> ApproveClaim(Guid id, [FromBody] ApproveRejectClaimRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.ApproveOrRejectClaimAsync(id, request, officerId);
        return Ok();
    }

    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/settle")]
    public async Task<IActionResult> MarkSettled(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.MarkClaimAsSettledAsync(id, officerId);
        return Ok();
    }

    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/assign-surveyor")]
    public async Task<IActionResult> AssignSurveyor(Guid id, [FromBody] AssignSurveyorRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.AssignSurveyorAsync(id, request.SurveyorId, officerId, request.Notes);
        return Ok();
    }

    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPost("{id}/request-docs")]
    public async Task<IActionResult> RequestDocuments(Guid id, [FromBody] string details)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.RequestAdditionalDocumentsAsync(id, details, officerId);
        return Ok();
    }

    [Authorize(Roles = "ClaimsOfficer")]
    [HttpPut("{id}/approve-preauth")]
    public async Task<IActionResult> ApprovePreAuth(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var officerId)) return Unauthorized();
        await _claimService.ApproveCashlessPreAuthAsync(id, officerId);
        return Ok();
    }

    // --- Surveyor Endpoints ---
    [Authorize(Roles = "Surveyor")]
    [HttpGet("surveyor/assigned")]
    public async Task<IActionResult> GetAssignedMotorClaims()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var surveyorId)) return Unauthorized();
        var result = await _claimService.GetAssignedMotorClaimsAsync(surveyorId);
        return Ok(result);
    }

    [Authorize(Roles = "Surveyor,ClaimsOfficer")]
    [HttpPost("{id}/survey-report")]
    public async Task<IActionResult> UploadSurveyReport(Guid id, [FromForm] SubmitSurveyReportRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await _claimService.SubmitSurveyReportAsync(id, userId, request);
        return Ok();
    }
}
