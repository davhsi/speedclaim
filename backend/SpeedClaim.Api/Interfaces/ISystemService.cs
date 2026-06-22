using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.SystemManagement;

namespace SpeedClaim.Api.Interfaces;

public interface ISystemService
{
    Task UpdateSystemConfigAsync(UpdateSystemConfigRequest request, Guid adminId);
    Task<IEnumerable<SystemConfigDto>> GetSystemConfigsAsync();
    Task<IEnumerable<AuditLogDto>> GetAuditLogsAsync();
    Task<IEnumerable<NotificationDto>> GetNotificationsAndEmailLogsAsync();
    Task ManageEmailTemplatesAsync(ManageEmailTemplateRequest request, Guid adminId);
    Task<IEnumerable<EmailTemplateDto>> GetEmailTemplatesAsync();
}
