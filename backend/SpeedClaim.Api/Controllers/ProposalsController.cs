using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.Sales;
using SpeedClaim.Api.Interfaces;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize]
public class ProposalsController : BaseApiController
{
    private readonly IProposalService _proposalService;

    public ProposalsController(IProposalService proposalService)
    {
        _proposalService = proposalService;
    }

    [Authorize(Roles = "Customer,Agent")]
    [HttpPost("quote")]
    public async Task<IActionResult> GenerateQuote([FromBody] GenerateQuoteRequest request)
    {
        var result = await _proposalService.GenerateQuoteAsync(request);
        return Ok(result);
    }

    [Authorize(Roles = "Customer,Agent")]
    [HttpPost]
    public async Task<IActionResult> SubmitProposal([FromBody] SubmitProposalRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (userId == null) return Unauthorized();
        var isAgent = User.IsInRole("Agent");
        var result = await _proposalService.SubmitProposalAsync(userId, request, isAgent);
        return Ok(result);
    }

    [Authorize(Roles = "Customer,Agent")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyProposals()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (userId == null) return Unauthorized();
        var isAgent = User.IsInRole("Agent");
        var result = await _proposalService.GetMyProposalsAsync(userId, isAgent);
        return Ok(result);
    }

    [Authorize(Roles = "Customer,Agent,Underwriter,Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProposalById(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole("Underwriter") || User.IsInRole("Admin");
        var result = await _proposalService.GetByIdAsync(id, userId, isAdmin);
        return Ok(result);
    }

    [Authorize(Roles = "Customer,Agent")]
    [HttpPost("{id}/upload")]
    public async Task<IActionResult> UploadDocument(string id, [FromForm] UploadDocumentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _proposalService.UploadDocumentAsync(id, userId, request.DocumentType, request.File);
        return Ok(new { FilePath = result });
    }

    [Authorize(Roles = "Underwriter,Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllProposals()
    {
        var result = await _proposalService.GetAllProposalsAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Underwriter")]
    [HttpPost("{id}/review")]
    public async Task<IActionResult> ReviewProposal(string id, [FromBody] ApproveRejectProposalRequest request)
    {
        var underwriterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (underwriterId == null) return Unauthorized();
        await _proposalService.ApproveOrRejectProposalAsync(id, underwriterId, request.IsApproved, request.Notes);
        return Ok();
    }

    [Authorize(Roles = "Underwriter")]
    [HttpPost("{id}/request-docs")]
    public async Task<IActionResult> RequestDocuments(string id, [FromBody] AdditionalDocumentRequest request)
    {
        var underwriterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (underwriterId == null) return Unauthorized();
        await _proposalService.RequestAdditionalDocumentsAsync(id, underwriterId, request.Details);
        return Ok();
    }

    [Authorize(Roles = "Underwriter")]
    [HttpPut("{id}/notes")]
    public async Task<IActionResult> AddNotes(string id, [FromBody] AddUnderwriterNotesRequest request)
    {
        var underwriterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (underwriterId == null) return Unauthorized();
        await _proposalService.AddUnderwriterNotesAsync(id, underwriterId, request.Notes);
        return Ok();
    }
}
