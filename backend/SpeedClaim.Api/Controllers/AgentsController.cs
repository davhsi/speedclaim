using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Interfaces;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize(Roles = "Agent, Admin")]
public class AgentsController : BaseApiController
{
    private readonly IAgentService _agentService;

    public AgentsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [Authorize(Roles = "Agent")]
    [HttpGet("customers")]
    public async Task<IActionResult> GetMyCustomers()
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        var result = await _agentService.GetAssignedCustomersAsync(agentId);
        return Ok(result);
    }

    [Authorize(Roles = "Agent")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        var result = await _agentService.GetAgentDashboardAsync(agentId);
        return Ok(result);
    }

    [Authorize(Roles = "Agent")]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        var result = await _agentService.GetAgentProfileAsync(agentId);
        return Ok(result);
    }

    [Authorize(Roles = "Agent")]
    [HttpGet("renewals")]
    public async Task<IActionResult> GetRenewalReminders()
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        var result = await _agentService.GetRenewalRemindersAsync(agentId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        var result = await _agentService.CreateBranchAsync(request, adminId);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches()
    {
        var result = await _agentService.GetBranchesAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{agentId}/branch/{branchId}")]
    public async Task<IActionResult> AssignAgentToBranch(string agentId, string branchId)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _agentService.AssignAgentToBranchAsync(agentId, branchId, adminId);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{agentId}/license")]
    public async Task<IActionResult> UpdateAgentLicense(string agentId, [FromBody] UpdateAgentLicenseRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _agentService.UpdateAgentLicenseAsync(agentId, request, adminId);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{agentId}/status")]
    public async Task<IActionResult> ActivateDeactivateAgent(string agentId, [FromQuery] bool isActive)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _agentService.ActivateDeactivateAgentAsync(agentId, isActive, adminId);
        return Ok();
    }
}
