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
[Authorize(Roles = "Customer")]
[EnableRateLimiting("policy-qa")]
public sealed class SpeedyAssistantController : ControllerBase
{
    private readonly ISpeedyAssistantService _service;
    public SpeedyAssistantController(ISpeedyAssistantService service) => _service = service;

    [HttpPost("messages")]
    public async Task<ActionResult<SpeedyAssistantResponse>> Ask([FromBody] AskSpeedyRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        return Ok(await _service.AnswerAsync(userId, request.Question, cancellationToken));
    }
}
