using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SpeedClaim.Api.Dtos.Assistant;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assistant")]
[EnableRateLimiting("policy-qa")]
public sealed class SpeedyAssistantController : ControllerBase
{
    private readonly ISpeedyAssistantService _service;
    public SpeedyAssistantController(ISpeedyAssistantService service) => _service = service;

    [HttpPost("messages")]
    [AllowAnonymous]
    public async Task<ActionResult<SpeedyAssistantResponse>> Ask([FromBody] AskSpeedyRequest request, CancellationToken cancellationToken)
    {
        Guid? customerUserId = User.IsInRole("Customer") && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
        return Ok(await _service.AnswerAsync(customerUserId, request.Question, cancellationToken));
    }

    [HttpPost("workspace/messages")]
    [AllowAnonymous]
    public async Task<ActionResult<SpeedyWorkspaceResponse>> AskWorkspace([FromBody] AskSpeedyWorkspaceRequest request, CancellationToken cancellationToken)
    {
        Guid? customerUserId = User.IsInRole("Customer") && Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
        return Ok(await _service.AnswerWorkspaceAsync(customerUserId, request.ConversationId, request.Question, cancellationToken));
    }

    [HttpGet("workspace/conversations")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<IReadOnlyList<SpeedyWorkspaceConversationDto>>> ListWorkspaceConversations() =>
        Ok(await _service.ListWorkspaceConversationsAsync(ActorId()));

    [HttpGet("workspace/conversations/{conversationId:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<SpeedyWorkspaceConversationDto>> GetWorkspaceConversation(Guid conversationId) =>
        Ok(await _service.GetWorkspaceConversationAsync(ActorId(), conversationId));

    private Guid ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new UnauthorizedAccessException();
}
