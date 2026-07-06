using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.SystemManagement;

namespace SpeedClaim.Api.Hubs;

public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
}
