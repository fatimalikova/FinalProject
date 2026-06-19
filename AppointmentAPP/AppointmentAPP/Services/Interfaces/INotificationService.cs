using AppointmentAPP.Dtos.NotificationDtos;
using AppointmentAPP.Enums;

namespace AppointmentAPP.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateAsync(Guid userId, string title, string message, NotificationType type);
        Task<List<ResponseNotificationDto>> GetMyNotificationsAsync(Guid userId);
        Task MarkAsReadAsync(Guid userId, Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
    }
}
