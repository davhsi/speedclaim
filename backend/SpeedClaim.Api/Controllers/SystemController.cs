using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Dtos.SystemManagement;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

[Authorize(Roles = "Admin")]
public class SystemController : BaseApiController
{
    private readonly ISystemService _systemService;

    public SystemController(ISystemService systemService)
    {
        _systemService = systemService;
    }

    #region Configuration

    /// <summary>Get all system configuration key-value pairs</summary>
    [HttpGet("configs")]
    [ProducesResponseType(typeof(IEnumerable<SystemConfigDto>), 200)]
    public async Task<IActionResult> GetConfigs()
    {
        var result = await _systemService.GetSystemConfigsAsync();
        return Ok(result);
    }

    /// <summary>Create or update a system configuration value</summary>
    [HttpPut("configs")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateSystemConfigRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId)) return Unauthorized();
        await _systemService.UpdateSystemConfigAsync(request, adminId);
        return Ok();
    }

    #endregion

    #region Logs

    /// <summary>Get the full audit log of admin and system actions</summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAuditLogs()
    {
        var result = await _systemService.GetAuditLogsAsync();
        return Ok(result);
    }

    /// <summary>Get a combined log of all notifications and email sends</summary>
    [HttpGet("notifications-logs")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetNotificationLogs()
    {
        var result = await _systemService.GetNotificationsAndEmailLogsAsync();
        return Ok(result);
    }

    #endregion

    #region Templates

    /// <summary>Create or update an email template by key</summary>
    [HttpPut("email-templates")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ManageEmailTemplates([FromBody] ManageEmailTemplateRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId)) return Unauthorized();
        await _systemService.ManageEmailTemplatesAsync(request, adminId);
        return Ok();
    }

    #endregion
}
