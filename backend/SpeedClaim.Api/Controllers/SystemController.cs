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
    [HttpPatch("configs")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateSystemConfigRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId)) return Unauthorized();
        await _systemService.UpdateSystemConfigAsync(request, adminId);
        return Ok(new { message = "System configuration updated." });
    }

    #endregion

    #region Logs

    /// <summary>Get the full audit log of admin and system actions</summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _systemService.GetAuditLogsAsync(page, pageSize, search, from, to);
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

    /// <summary>Get all email templates</summary>
    [HttpGet("email-templates")]
    [ProducesResponseType(typeof(IEnumerable<EmailTemplateDto>), 200)]
    public async Task<IActionResult> GetEmailTemplates()
    {
        var result = await _systemService.GetEmailTemplatesAsync();
        return Ok(result);
    }

    /// <summary>Create or update an email template by key</summary>
    [HttpPatch("email-templates")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ManageEmailTemplates([FromBody] ManageEmailTemplateRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId)) return Unauthorized();
        await _systemService.ManageEmailTemplatesAsync(request, adminId);
        return Ok(new { message = "Email template saved successfully." });
    }

    #endregion
}
