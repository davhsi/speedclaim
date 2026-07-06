using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SpeedClaim.Api.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    public static string GroupForUser(Guid userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupForUser(userId));
        }

        await base.OnConnectedAsync();
    }
}
