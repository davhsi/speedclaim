using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.SystemManagement;

namespace SpeedClaim.Api.Interfaces;

public interface INotificationService
{
    Task CreateAsync(Guid userId, string title, string message, string type, string? redirectUrl = null);
    Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}
