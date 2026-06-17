using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Auth;
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

    #region Agent Endpoints

    /// <summary>Get all customers assigned to the authenticated agent via proposals</summary>
    [Authorize(Roles = "Agent")]
    [HttpGet("customers")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public async Task<IActionResult> GetMyCustomers()
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        var result = await _agentService.GetAssignedCustomersAsync(agentId);
        return Ok(result);
    }

    /// <summary>Get dashboard summary for the authenticated agent — customers, policies, commissions, pending claims</summary>
    [Authorize(Roles = "Agent")]
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AgentDashboardDto), 200)]
    public async Task<IActionResult> GetDashboard()
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        var result = await _agentService.GetAgentDashboardAsync(agentId);
        return Ok(result);
    }

    /// <summary>Get the profile of the authenticated agent including branch and license details</summary>
    [Authorize(Roles = "Agent")]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(AgentProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfile()
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        var result = await _agentService.GetAgentProfileAsync(agentId);
        return Ok(result);
    }

    /// <summary>Update the authenticated agent's own contact details (name, phone)</summary>
    [Authorize(Roles = "Agent")]
    [HttpPut("profile")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateAgentProfileRequest request)
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        await _agentService.UpdateAgentProfileAsync(agentId, request);
        return Ok(new { message = "Agent profile updated successfully." });
    }

    /// <summary>Get policies with premiums due within the next 30 days for the agent's customers</summary>
    [Authorize(Roles = "Agent")]
    [HttpGet("renewals")]
    [ProducesResponseType(typeof(IEnumerable<RenewalReminderDto>), 200)]
    public async Task<IActionResult> GetRenewalReminders()
    {
        var agentId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (agentId == null) return Unauthorized();
        var result = await _agentService.GetRenewalRemindersAsync(agentId);
        return Ok(result);
    }

    #endregion

    #region Admin Endpoints

    /// <summary>Admin — create a new branch office</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("branches")]
    [ProducesResponseType(typeof(BranchDto), 200)]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        var result = await _agentService.CreateBranchAsync(request, adminId);
        return Ok(result);
    }

    /// <summary>Admin — get all branch offices</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("branches")]
    [ProducesResponseType(typeof(IEnumerable<BranchDto>), 200)]
    public async Task<IActionResult> GetBranches()
    {
        var result = await _agentService.GetBranchesAsync();
        return Ok(result);
    }

    /// <summary>Admin — assign an agent to a branch</summary>
    /// <param name="agentId">Agent user ID</param>
    /// <param name="branchId">Branch ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{agentId}/branch/{branchId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignAgentToBranch(string agentId, string branchId)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _agentService.AssignAgentToBranchAsync(agentId, branchId, adminId);
        return Ok(new { message = "Agent assigned to branch successfully." });
    }

    /// <summary>Admin — update an agent's license number and expiry date</summary>
    /// <param name="agentId">Agent user ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{agentId}/license")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAgentLicense(string agentId, [FromBody] UpdateAgentLicenseRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _agentService.UpdateAgentLicenseAsync(agentId, request, adminId);
        return Ok(new { message = "Agent license details updated." });
    }

    /// <summary>Admin — activate or deactivate an agent account</summary>
    /// <param name="agentId">Agent user ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{agentId}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ActivateDeactivateAgent(string agentId, [FromQuery] bool isActive)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (adminId == null) return Unauthorized();
        await _agentService.ActivateDeactivateAgentAsync(agentId, isActive, adminId);
        return Ok(new { message = isActive ? "Agent account activated." : "Agent account deactivated." });
    }

    #endregion
}
