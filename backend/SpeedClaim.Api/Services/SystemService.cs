using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        var config = await _unitOfWork.SystemConfigs.SingleOrDefaultAsync(c => c.ConfigKey == request.ConfigKey);
        
        if (config == null)
        {
            config = new SystemConfig
            {
                Id = Guid.NewGuid(),
                ConfigKey = request.ConfigKey,
                ConfigValue = request.ConfigValue
            };
            await _unitOfWork.SystemConfigs.AddAsync(config);
        }
        else
        {
            config.ConfigValue = request.ConfigValue;
            config.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.SystemConfigs.Update(config);
        }

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

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync()
    {
        var logs = await _unitOfWork.AuditLogs.GetAllAsync();
        // Since we don't have pagination yet, we might want to order it descending and take top 100 or something in real app.
        // For now, we return all as per existing repo method signature.
        return logs.Select(l => new AuditLogDto(
            l.Id,
            l.EntityType,
            l.EntityId,
            l.Action,
            l.OldValue,
            l.NewValue,
            l.UserId,
            l.IpAddress,
            l.CreatedAt
        ));
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
            n.CreatedAt
        ));
    }

    public async Task ManageEmailTemplatesAsync(ManageEmailTemplateRequest request, Guid adminId)
    {
        var template = await _unitOfWork.EmailTemplates.SingleOrDefaultAsync(t => t.TemplateKey == request.TemplateKey);

        if (template == null)
        {
            template = new EmailTemplate
            {
                Id = Guid.NewGuid(),
                TemplateKey = request.TemplateKey,
                Subject = request.Subject,
                BodyHtml = request.BodyHtml,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.EmailTemplates.AddAsync(template);
        }
        else
        {
            template.Subject = request.Subject;
            template.BodyHtml = request.BodyHtml;
            template.UpdatedAt = DateTimeOffset.UtcNow;
            _unitOfWork.EmailTemplates.Update(template);
        }

        await _unitOfWork.CompleteAsync();
    }
}
