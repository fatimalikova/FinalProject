using AppointmentAPP.Data;
using AppointmentAPP.Enums;
using AppointmentAPP.Interfaces;
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
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await appointmentService.AutoCompletePastAppointmentsAsync();

                var setting = await db.SystemSettings.FirstOrDefaultAsync(stoppingToken);
                var reminderHours = setting?.ReminderHoursBeforeAppointment ?? 24;

                var now = DateTime.UtcNow;
                var windowEnd = now.AddHours(reminderHours);

                var dueAppointments = await db.Appointments
                    .Include(a => a.Client)
                    .Include(a => a.Provider).ThenInclude(p => p.User)
                    .Include(a => a.Service)
                    .Where(a => a.Status == AppointmentStatus.Confirmed
                                && !a.ReminderSent
                                && a.StartDateTime > now
                                && a.StartDateTime <= windowEnd)
                    .ToListAsync(stoppingToken);

                foreach (var appointment in dueAppointments)
                {
                    // ===== Client-ə reminder (in-app) =====
                    await notificationService.CreateAsync(
                        appointment.ClientId, "Appointment Reminder",
                        $"Reminder: your {appointment.Service.Name} appointment is at {appointment.StartDateTime:dd MMM yyyy HH:mm}.",
                        NotificationType.AppointmentReminder);

                    // ===== Provider-ə reminder (in-app) =====
                    await notificationService.CreateAsync(
                        appointment.Provider.UserId, "Upcoming Appointment",
                        $"Reminder: {appointment.Client.FullName} has an appointment for {appointment.Service.Name} at {appointment.StartDateTime:dd MMM yyyy HH:mm}.",
                        NotificationType.AppointmentReminder);

                    // ===== Provider-ə reminder (email) =====
                    try
                    {
                        await emailService.SendEmailAsync(
                            appointment.Provider.User.Email!,
                            "Upcoming Appointment Reminder",
                            $"<h3>Yaxınlaşan randevu</h3>" +
                            $"<p><b>{appointment.Client.FullName}</b> ilə <b>{appointment.Service.Name}</b> randevunuz var.</p>" +
                            $"<p>Vaxt: <b>{appointment.StartDateTime:dd MMM yyyy HH:mm}</b></p>");
                    }
                    catch
                    {
                        // email fail olsa belə, reminder prosesi davam etməlidir
                    }

                    appointment.ReminderSent = true;
                }

                if (dueAppointments.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}