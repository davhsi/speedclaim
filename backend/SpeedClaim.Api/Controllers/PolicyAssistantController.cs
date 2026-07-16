using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/policies/{policyId:guid}/assistant")]
[Authorize(Roles = "Customer,Underwriter,Admin")]
[EnableRateLimiting("policy-qa")]
public sealed class PolicyAssistantController : ControllerBase
{
    private readonly IPolicyAssistantService _service;
    public PolicyAssistantController(IPolicyAssistantService service) => _service = service;

    [HttpGet("availability")]
    public async Task<ActionResult<PolicyAssistantAvailabilityDto>> Availability(Guid policyId) =>
        Ok(await _service.GetAvailabilityAsync(policyId, ActorId(), User.IsInRole("Customer")));

    [HttpGet("conversations")]
    public async Task<ActionResult<IReadOnlyList<PolicyAssistantConversationDto>>> List(Guid policyId) =>
        Ok(await _service.ListAsync(policyId, ActorId(), User.IsInRole("Customer")));

    [HttpPost("conversations")]
    public async Task<ActionResult<PolicyAssistantConversationDto>> Create(Guid policyId, [FromBody] CreatePolicyAssistantConversationRequest request) =>
        Ok(await _service.CreateAsync(policyId, ActorId(), User.IsInRole("Customer")));

    [HttpGet("conversations/{conversationId}")]
    public async Task<ActionResult<PolicyAssistantConversationDto>> Get(Guid policyId, Guid conversationId) =>
        Ok(await _service.GetAsync(policyId, conversationId, ActorId(), User.IsInRole("Customer")));

    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<ActionResult<PolicyAssistantAnswerDto>> Send(Guid policyId, Guid conversationId, [FromBody] SendPolicyAssistantMessageRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.SendAsync(policyId, conversationId, request.Question, ActorId(), User.IsInRole("Customer"), cancellationToken));

    private Guid ActorId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : throw new UnauthorizedAccessException();
}
