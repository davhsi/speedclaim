using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.SystemManagement;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public class SystemService : ISystemService
{
    private readonly IUnitOfWork _unitOfWork;

    public SystemService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task UpdateSystemConfigAsync(UpdateSystemConfigRequest request, Guid adminId)
    {
        var key = request.ConfigKey.Trim();
        var value = request.ConfigValue.Trim();
        var config = await _unitOfWork.SystemConfigs.SingleOrDefaultAsync(c => c.ConfigKey == key);
        var oldValue = config?.ConfigValue;
        
        if (config == null)
        {
            config = new SystemConfig
            {
                Id = Guid.NewGuid(),
                ConfigKey = key,
                ConfigValue = value,
                UpdatedById = adminId,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.SystemConfigs.AddAsync(config);
        }
        else
        {
            config.ConfigValue = value;
            config.UpdatedById = adminId;
            config.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.SystemConfigs.Update(config);
        }

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            EntityType = "SystemConfig",
            EntityId = config.Id,
            Action = oldValue == null ? "SystemConfigCreated" : "SystemConfigUpdated",
            OldValue = oldValue == null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = JsonSerializer.Serialize(value),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<SystemConfigDto>> GetSystemConfigsAsync()
    {
        var configs = await _unitOfWork.SystemConfigs.GetAllAsync();
        return configs.Select(c => new SystemConfigDto(
            c.ConfigKey,
            c.ConfigValue,
            c.Description,
            c.UpdatedAt
        ));
    }

    public async Task<PagedResponse<AuditLogDto>> GetAuditLogsAsync(int page, int pageSize, string? search, DateTime? from, DateTime? to)
    {
        var allLogs = (await _unitOfWork.AuditLogs.GetAllAsync()).OrderByDescending(l => l.CreatedAt);
        var users = await _unitOfWork.Users.GetAllAsync();
        var nameById = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());

        var dtos = allLogs.Select(l => new AuditLogDto(
            l.Id, l.EntityType, l.EntityId, l.Action, l.OldValue, l.NewValue, l.UserId,
            l.UserId.HasValue && nameById.TryGetValue(l.UserId.Value, out var n) ? n : null,
            l.IpAddress, l.CreatedAt));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            dtos = dtos.Where(l =>
                l.Action.ToLower().Contains(term) ||
                l.EntityType.ToLower().Contains(term) ||
                (l.UserName != null && l.UserName.ToLower().Contains(term)));
        }
        if (from.HasValue)
            dtos = dtos.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue)
            dtos = dtos.Where(l => l.CreatedAt < to.Value.AddDays(1));

        var total = dtos.Count();
        var paged = dtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResponse<AuditLogDto>(paged, page, pageSize, total);
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAndEmailLogsAsync()
    {
        var notifications = await _unitOfWork.Notifications.GetAllAsync();
        return notifications.Select(n => new NotificationDto(
            n.Id,
            n.UserId,
            n.Title,
            n.Message,
            n.Type,
            n.IsRead,
            n.CreatedAt,
            n.RedirectUrl
        ));
    }

    public async Task ManageEmailTemplatesAsync(ManageEmailTemplateRequest request, Guid adminId)
    {
        var key = request.TemplateKey.Trim();
        var subject = request.Subject.Trim();
        var bodyHtml = request.BodyHtml.Trim();
        var template = await _unitOfWork.EmailTemplates.SingleOrDefaultAsync(t => t.TemplateKey == key);
        var oldValue = template == null ? null : JsonSerializer.Serialize(new { template.Subject, template.BodyHtml });

        if (template == null)
        {
            template = new EmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateKey = key,
                Subject = subject,
                BodyHtml = bodyHtml,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.EmailTemplates.AddAsync(template);
        }
        else
        {
            template.Subject = subject;
            template.BodyHtml = bodyHtml;
            template.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.EmailTemplates.Update(template);
        }

        await _unitOfWork.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = adminId,
            EntityType = "EmailTemplate",
            EntityId = template.Id,
            Action = oldValue == null ? "EmailTemplateCreated" : "EmailTemplateUpdated",
            OldValue = oldValue,
            NewValue = JsonSerializer.Serialize(new { template.Subject, template.BodyHtml }),
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.CompleteAsync();
    }

    public async Task<IEnumerable<EmailTemplateDto>> GetEmailTemplatesAsync()
    {
        var templates = await _unitOfWork.EmailTemplates.GetAllAsync();
        return templates.Select(t => new EmailTemplateDto(
            t.Id,
            t.TemplateKey,
            t.Subject,
            t.BodyHtml,
            t.IsActive,
            t.CreatedAt,
            t.UpdatedAt
        ));
    }
}
