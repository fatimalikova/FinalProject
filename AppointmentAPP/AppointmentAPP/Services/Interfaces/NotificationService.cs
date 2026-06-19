using AppointmentAPP.Data;
using AppointmentAPP.Dtos.NotificationDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services.Interfaces
{
    public class NotificationService(AppDbContext db) : INotificationService
    {
        public async Task CreateAsync(Guid userId, string title, string message, NotificationType type)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type
            });
            await db.SaveChangesAsync();
        }

        public async Task<List<ResponseNotificationDto>> GetMyNotificationsAsync(Guid userId)
        {
            var notifications = await db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications.Select(n => new ResponseNotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();
        }

        public async Task MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
                ?? throw new NotFoundException("Notification not found.");

            notification.IsRead = true;
            await db.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in notifications) n.IsRead = true;
            await db.SaveChangesAsync();
        }
    }
}
