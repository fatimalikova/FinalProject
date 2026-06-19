using AppointmentAPP.Data;
using AppointmentAPP.Enums;
using AppointmentAPP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class ReminderBackgroundService(IServiceScopeFactory scopeFactory) : BackgroundService
    {
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(10);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();

                // 1) keçmiş Confirmed appointment-ləri Completed et
                await appointmentService.AutoCompletePastAppointmentsAsync();

                // 2) reminder göndərilməli olanları tap
                var setting = await db.SystemSettings.FirstOrDefaultAsync(stoppingToken);
                var reminderHours = setting?.ReminderHoursBeforeAppointment ?? 24;

                var now = DateTime.UtcNow;
                var windowEnd = now.AddHours(reminderHours);

                var dueAppointments = await db.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Provider)
                    .Include(a => a.Service)
                    .Where(a => a.Status == AppointmentStatus.Confirmed
                                && !a.ReminderSent
                                && a.StartDateTime > now
                                && a.StartDateTime <= windowEnd)
                    .ToListAsync(stoppingToken);

                foreach (var appointment in dueAppointments)
                {
                    await notificationService.CreateAsync(
                        appointment.ClientId, "Appointment Reminder",
                        $"Reminder: your {appointment.Service.Name} appointment is at {appointment.StartDateTime:dd MMM yyyy HH:mm}.",
                        NotificationType.AppointmentReminder);

                    appointment.ReminderSent = true;
                }

                if (dueAppointments.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}