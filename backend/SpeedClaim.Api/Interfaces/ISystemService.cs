using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.SystemManagement;

namespace SpeedClaim.Api.Interfaces;

public interface ISystemService
{
    Task UpdateSystemConfigAsync(UpdateSystemConfigRequest request, Guid adminId);
    Task<IEnumerable<SystemConfigDto>> GetSystemConfigsAsync();
    Task<PagedResponse<AuditLogDto>> GetAuditLogsAsync(int page, int pageSize, string? search, DateTime? from, DateTime? to);
    Task<IEnumerable<NotificationDto>> GetNotificationsAndEmailLogsAsync();
    Task ManageEmailTemplatesAsync(ManageEmailTemplateRequest request, Guid adminId);
    Task<IEnumerable<EmailTemplateDto>> GetEmailTemplatesAsync();
}
