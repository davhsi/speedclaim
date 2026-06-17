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

    #region Customer / Agent Endpoints

    /// <summary>Generate a premium quote for a given product, age, sum assured, and tenure</summary>
    /// <remarks>Does not create any record — purely a calculation based on the product's rate table.</remarks>
    [Authorize(Roles = "Customer,Agent")]
    [HttpPost("quote")]
    [ProducesResponseType(typeof(GenerateQuoteResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GenerateQuote([FromBody] GenerateQuoteRequest request)
    {
        var result = await _proposalService.GenerateQuoteAsync(request);
        return Ok(result);
    }

    /// <summary>Submit a new insurance proposal</summary>
    /// <remarks>
    /// Creates a proposal in Submitted status. Supports Health, Life, and Motor domains.
    /// Agents submitting on behalf of a customer must include the customer's ID.
    /// </remarks>
    [Authorize(Roles = "Customer,Agent")]
    [HttpPost]
    [ProducesResponseType(typeof(ProposalDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SubmitProposal([FromBody] SubmitProposalRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (userId == null) return Unauthorized();
        var isAgent = User.IsInRole("Agent");
        var result = await _proposalService.SubmitProposalAsync(userId, request, isAgent);
        return Ok(result);
    }

    /// <summary>Get all proposals for the authenticated customer or agent</summary>
    [Authorize(Roles = "Customer,Agent")]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<ProposalDto>), 200)]
    public async Task<IActionResult> GetMyProposals()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (userId == null) return Unauthorized();
        var isAgent = User.IsInRole("Agent");
        var result = await _proposalService.GetMyProposalsAsync(userId, isAgent);
        return Ok(result);
    }

    /// <summary>Get a proposal by ID. Customers and agents can only access proposals they own</summary>
    /// <param name="id">Proposal ID</param>
    [Authorize(Roles = "Customer,Agent,Underwriter,Admin")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProposalDto), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProposalById(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = User.IsInRole("Underwriter") || User.IsInRole("Admin");
        var result = await _proposalService.GetByIdAsync(id, userId, isAdmin);
        return Ok(result);
    }

    /// <summary>Upload (or replace) a supporting document for a proposal</summary>
    /// <param name="id">Proposal ID</param>
    /// <param name="documentKey">Document type key (e.g. ID_PROOF, INCOME_PROOF)</param>
    [Authorize(Roles = "Customer,Agent")]
    [HttpPut("{id}/documents/{documentKey}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UploadDocument(string id, string documentKey, [FromForm] UploadDocumentRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _proposalService.UploadDocumentAsync(id, userId, documentKey, request.File);
        return Ok(new { FilePath = result });
    }

    #endregion

    #region Underwriter Endpoints

    /// <summary>Underwriter — get all submitted proposals pending review</summary>
    [Authorize(Roles = "Underwriter,Admin")]
    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<ProposalDto>), 200)]
    public async Task<IActionResult> GetAllProposals()
    {
        var result = await _proposalService.GetAllProposalsAsync();
        return Ok(result);
    }

    /// <summary>Underwriter — approve or reject a proposal</summary>
    /// <remarks>Approval creates a Policy (in Pending status) and a first premium schedule entry.</remarks>
    /// <param name="id">Proposal ID</param>
    [Authorize(Roles = "Underwriter")]
    [HttpPost("{id}/review")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ReviewProposal(string id, [FromBody] ApproveRejectProposalRequest request)
    {
        var underwriterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (underwriterId == null) return Unauthorized();
        await _proposalService.ApproveOrRejectProposalAsync(id, underwriterId, request.IsApproved, request.Notes);
        return Ok(new { message = request.IsApproved ? "Proposal approved. Policy has been created." : "Proposal rejected." });
    }

    /// <summary>Underwriter — request additional documents from the customer before deciding</summary>
    /// <param name="id">Proposal ID</param>
    [Authorize(Roles = "Underwriter")]
    [HttpPost("{id}/request-docs")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RequestDocuments(string id, [FromBody] AdditionalDocumentRequest request)
    {
        var underwriterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (underwriterId == null) return Unauthorized();
        await _proposalService.RequestAdditionalDocumentsAsync(id, underwriterId, request.Details);
        return Ok(new { message = "Additional document request sent to the customer." });
    }

    /// <summary>Underwriter — append internal review notes to a proposal</summary>
    /// <param name="id">Proposal ID</param>
    [Authorize(Roles = "Underwriter")]
    [HttpPut("{id}/notes")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddNotes(string id, [FromBody] AddUnderwriterNotesRequest request)
    {
        var underwriterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (underwriterId == null) return Unauthorized();
        await _proposalService.AddUnderwriterNotesAsync(id, underwriterId, request.Notes);
        return Ok(new { message = "Underwriter notes saved." });
    }

    #endregion
}
