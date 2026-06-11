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

    [HttpGet("configs")]
    public async Task<IActionResult> GetConfigs()
    {
        var result = await _systemService.GetSystemConfigsAsync();
        return Ok(result);
    }

    [HttpPut("configs")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateSystemConfigRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId)) return Unauthorized();
        await _systemService.UpdateSystemConfigAsync(request, adminId);
        return Ok();
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var result = await _systemService.GetAuditLogsAsync();
        return Ok(result);
    }

    [HttpGet("notifications-logs")]
    public async Task<IActionResult> GetNotificationLogs()
    {
        var result = await _systemService.GetNotificationsAndEmailLogsAsync();
        return Ok(result);
    }

    [HttpPut("email-templates")]
    public async Task<IActionResult> ManageEmailTemplates([FromBody] ManageEmailTemplateRequest request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminId)) return Unauthorized();
        await _systemService.ManageEmailTemplatesAsync(request, adminId);
        return Ok();
    }
}
