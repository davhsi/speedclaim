using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SpeedClaim.Api.Dtos.SystemManagement;
using SpeedClaim.Api.Exceptions;
using SpeedClaim.Api.Hubs;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

    public NotificationService(IUnitOfWork unitOfWork, IHubContext<NotificationHub, INotificationClient> hubContext)
    {
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
    }

    public async Task CreateAsync(Guid userId, string title, string message, string type, string? redirectUrl = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            RedirectUrl = redirectUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.CompleteAsync();

        var dto = new NotificationDto(notification.Id, notification.UserId, notification.Title,
            notification.Message, notification.Type, notification.IsRead, notification.CreatedAt, notification.RedirectUrl);
        await _hubContext.Clients.Group(NotificationHub.GroupForUser(userId)).ReceiveNotification(dto);
    }

    public async Task<IEnumerable<NotificationDto>> GetForUserAsync(Guid userId)
    {
        var notifications = await _unitOfWork.Notifications.FindAsync(n => n.UserId == userId);
        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.UserId, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt, n.RedirectUrl));
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
            throw new NotFoundException("Notification not found.");

        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.CompleteAsync();
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unread = await _unitOfWork.Notifications.FindAsync(n => n.UserId == userId && !n.IsRead);
        foreach (var n in unread)
        {
            n.IsRead = true;
            _unitOfWork.Notifications.Update(n);
        }
        await _unitOfWork.CompleteAsync();
    }
}
