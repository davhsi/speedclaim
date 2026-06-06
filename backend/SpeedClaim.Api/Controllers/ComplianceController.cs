using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Compliance;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Controllers;

[ApiVersion("1.0")]
public class ComplianceController : BaseApiController
{
    private readonly IUnitOfWork _unitOfWork;

    public ComplianceController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var (logs, totalCount) = await _unitOfWork.AuditLogs.GetPagedAsync(
            pageNumber,
            pageSize,
            null, // No filter
            query => query.OrderByDescending(a => a.CreatedAt)
        );

        var dtos = logs.Select(l => new AuditLogDto
        {
            Id = l.Id,
            ActorId = l.ActorId,
            EntityType = l.EntityType,
            EntityId = l.EntityId,
            Action = l.Action,
            OldValues = l.OldValues,
            NewValues = l.NewValues,
            IpAddress = l.IpAddress,
            UserAgent = l.UserAgent,
            CreatedAt = l.CreatedAt
        });

        return Ok(new Dtos.Common.PagedResponse<AuditLogDto>(dtos, pageNumber, pageSize, totalCount));
    }

    [HttpGet("user-consents")]
    [Authorize]
    public async Task<IActionResult> GetMyConsents()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var consents = await _unitOfWork.UserConsents.FindAsync(c => c.UserId == userId);
        var dtos = consents.Select(c => new UserConsentDto
        {
            Id = c.Id,
            UserId = c.UserId,
            ConsentType = c.ConsentType,
            IsGranted = c.IsGranted,
            ConsentVersion = c.ConsentVersion,
            IpAddress = c.IpAddress,
            GrantedAt = c.GrantedAt,
            WithdrawnAt = c.WithdrawnAt
        });

        return Ok(dtos);
    }

    [HttpPost("user-consents")]
    [Authorize]
    public async Task<IActionResult> UpdateConsent([FromBody] UpdateUserConsentRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var existing = (await _unitOfWork.UserConsents.FindAsync(c => c.UserId == userId && c.ConsentType == request.ConsentType)).FirstOrDefault();
        
        if (existing != null)
        {
            existing.IsGranted = request.IsGranted;
            existing.WithdrawnAt = request.IsGranted ? null : DateTime.UtcNow;
        }
        else
        {
            await _unitOfWork.UserConsents.AddAsync(new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ConsentType = request.ConsentType,
                IsGranted = request.IsGranted,
                ConsentVersion = "v1", // In real-world, get from config
                GrantedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.CompleteAsync();
        return Ok(new { message = "Consent updated successfully." });
    }
}
