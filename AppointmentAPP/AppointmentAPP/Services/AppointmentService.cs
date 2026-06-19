using AppointmentAPP.Data;
using AppointmentAPP.Dtos.AppointmentDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Models;
using AppointmentAPP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class AppointmentService(
        AppDbContext db,
        IAvailabilityService availabilityService,
        INotificationService notificationService
    ) : IAppointmentService
    {
        public async Task<AppointmentResponseDto> BookAsync(Guid clientId, BookAppointmentDto dto)
        {
            var service = await db.Services
                .Include(s => s.Provider)
                .FirstOrDefaultAsync(s => s.Id == dto.ServiceId && s.ProviderId == dto.ProviderId && s.IsActive)
                ?? throw new NotFoundException("Service not found for this provider.");

            if (service.Provider.Status != ProviderStatus.Approved || !service.Provider.IsActive)
                throw new BadRequestException("This provider is not currently accepting bookings.");

            var endDateTime = dto.StartDateTime.AddMinutes(service.DurationMinutes);

            var setting = await db.SystemSettings.FirstOrDefaultAsync();
            var maxAdvanceDays = setting?.MaxAdvanceBookingDays ?? 30;

            if (dto.StartDateTime > DateTime.UtcNow.AddDays(maxAdvanceDays))
                throw new BadRequestException($"Bookings can only be made up to {maxAdvanceDays} days in advance.");

            if (dto.StartDateTime < DateTime.UtcNow)
                throw new BadRequestException("Cannot book an appointment in the past.");

            var isAvailable = await availabilityService.IsSlotAvailableAsync(dto.ProviderId, dto.StartDateTime, endDateTime);
            if (!isAvailable)
                throw new BadRequestException("This time slot is no longer available.");

            // 🔑 Brief-ə uyğun olaraq — booking olan kimi Confirmed olur, manual approve mərhələsi yoxdur
            var appointment = new Appointment
            {
                ClientId = clientId,
                ProviderId = dto.ProviderId,
                ServiceId = dto.ServiceId,
                StartDateTime = dto.StartDateTime,
                EndDateTime = endDateTime,
                Status = AppointmentStatus.Confirmed,
                Notes = dto.Notes
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            await notificationService.CreateAsync(
                clientId, "Appointment Confirmed",
                $"Your appointment for {service.Name} on {dto.StartDateTime:dd MMM yyyy HH:mm} is confirmed.",
                NotificationType.AppointmentConfirmed);

            await notificationService.CreateAsync(
                service.Provider.UserId, "New Appointment Booked",
                $"A new appointment has been booked for {service.Name} on {dto.StartDateTime:dd MMM yyyy HH:mm}.",
                NotificationType.AppointmentBooked);

            return await MapToDto(appointment.Id);
        }

        public async Task<AppointmentResponseDto> CancelAsync(Guid userId, Guid appointmentId, CancelAppointmentDto dto)
        {
            var appointment = await GetAppointmentWithIncludesAsync(appointmentId);

            EnsureUserIsParticipant(appointment, userId);

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                throw new BadRequestException("This appointment cannot be cancelled.");

            await EnsureNoticeRespectedAsync(appointment);

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.CancelReason = dto.Reason;
            appointment.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var otherPartyUserId = userId == appointment.ClientId ? appointment.Provider.UserId : appointment.ClientId;
            await notificationService.CreateAsync(
                otherPartyUserId, "Appointment Cancelled",
                $"The appointment on {appointment.StartDateTime:dd MMM yyyy HH:mm} has been cancelled.",
                NotificationType.AppointmentCancelled);

            return MapToDtoFromEntity(appointment);
        }

        public async Task<AppointmentResponseDto> RescheduleAsync(Guid userId, Guid appointmentId, RescheduleAppointmentDto dto)
        {
            var appointment = await GetAppointmentWithIncludesAsync(appointmentId);

            EnsureUserIsParticipant(appointment, userId);

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                throw new BadRequestException("This appointment cannot be rescheduled.");

            await EnsureNoticeRespectedAsync(appointment);

            var newEnd = dto.NewStartDateTime.AddMinutes(appointment.Service.DurationMinutes);

            var setting = await db.SystemSettings.FirstOrDefaultAsync();
            var maxAdvanceDays = setting?.MaxAdvanceBookingDays ?? 30;
            if (dto.NewStartDateTime > DateTime.UtcNow.AddDays(maxAdvanceDays))
                throw new BadRequestException($"Bookings can only be made up to {maxAdvanceDays} days in advance.");

            var isAvailable = await availabilityService.IsSlotAvailableAsync(
                appointment.ProviderId, dto.NewStartDateTime, newEnd, excludeAppointmentId: appointment.Id);

            if (!isAvailable)
                throw new BadRequestException("The new time slot is not available.");

            appointment.StartDateTime = dto.NewStartDateTime;
            appointment.EndDateTime = newEnd;
            appointment.ReminderSent = false; // yeni vaxt üçün reminder yenidən göndərilməlidir
            appointment.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var otherPartyUserId = userId == appointment.ClientId ? appointment.Provider.UserId : appointment.ClientId;
            await notificationService.CreateAsync(
                otherPartyUserId, "Appointment Rescheduled",
                $"The appointment has been rescheduled to {dto.NewStartDateTime:dd MMM yyyy HH:mm}.",
                NotificationType.AppointmentConfirmed);

            return MapToDtoFromEntity(appointment);
        }

        public async Task<AppointmentResponseDto> CompleteAsync(Guid providerUserId, Guid appointmentId)
        {
            var appointment = await GetAppointmentWithIncludesAsync(appointmentId);

            if (appointment.Provider.UserId != providerUserId)
                throw new ForbiddenException("You can only complete your own appointments.");

            if (appointment.Status != AppointmentStatus.Confirmed)
                throw new BadRequestException("Only confirmed appointments can be marked as completed.");

            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return MapToDtoFromEntity(appointment);
        }

        // F7 — Client history (upcoming / past / all)
        public async Task<List<AppointmentResponseDto>> GetMyAppointmentsAsync(Guid clientUserId, string? filter)
        {
            var query = db.Appointments
                .Include(a => a.Client)
                .Include(a => a.Provider)
                .Include(a => a.Service)
                .Where(a => a.ClientId == clientUserId);

            var now = DateTime.UtcNow;
            query = filter?.ToLower() switch
            {
                "upcoming" => query.Where(a => a.StartDateTime >= now && a.Status != AppointmentStatus.Cancelled),
                "past" => query.Where(a => a.StartDateTime < now || a.Status == AppointmentStatus.Completed),
                _ => query
            };

            var appointments = await query.OrderByDescending(a => a.StartDateTime).ToListAsync();
            return appointments.Select(MapToDtoFromEntity).ToList();
        }

        // F5 — Provider calendar (daily/weekly — from/to ilə idarə olunur)
        public async Task<List<AppointmentResponseDto>> GetProviderCalendarAsync(Guid providerUserId, DateTime from, DateTime to)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == providerUserId)
                ?? throw new NotFoundException("Provider profile not found.");

            var appointments = await db.Appointments
                .Include(a => a.Client)
                .Include(a => a.Provider)
                .Include(a => a.Service)
                .Where(a => a.ProviderId == provider.Id
                            && a.StartDateTime.Date >= from.Date
                            && a.StartDateTime.Date <= to.Date
                            && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();

            return appointments.Select(MapToDtoFromEntity).ToList();
        }

        // Background job tərəfindən çağırılır — keçmiş Confirmed appointment-ləri Completed edir
        public async Task AutoCompletePastAppointmentsAsync()
        {
            var now = DateTime.UtcNow;
            var expired = await db.Appointments
                .Where(a => a.Status == AppointmentStatus.Confirmed && a.EndDateTime < now)
                .ToListAsync();

            foreach (var a in expired)
            {
                a.Status = AppointmentStatus.Completed;
                a.UpdatedAt = now;
            }

            if (expired.Count > 0)
                await db.SaveChangesAsync();
        }

        // ===== Helper-lər =====

        private async Task<Appointment> GetAppointmentWithIncludesAsync(Guid appointmentId)
        {
            return await db.Appointments
                .Include(a => a.Client)
                .Include(a => a.Provider)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == appointmentId)
                ?? throw new NotFoundException("Appointment not found.");
        }

        private static void EnsureUserIsParticipant(Appointment appointment, Guid userId)
        {
            var isClient = appointment.ClientId == userId;
            var isProvider = appointment.Provider.UserId == userId;

            if (!isClient && !isProvider)
                throw new ForbiddenException("You are not authorized to modify this appointment.");
        }

        private async Task EnsureNoticeRespectedAsync(Appointment appointment)
        {
            var setting = await db.SystemSettings.FirstOrDefaultAsync();
            var minNoticeHours = setting?.MinCancellationNoticeHours ?? 24;

            var hoursUntilAppointment = (appointment.StartDateTime - DateTime.UtcNow).TotalHours;

            if (hoursUntilAppointment < minNoticeHours)
                throw new BadRequestException(
                    $"This action requires at least {minNoticeHours} hours notice before the appointment.");
        }

        private async Task<AppointmentResponseDto> MapToDto(Guid appointmentId)
        {
            var appointment = await GetAppointmentWithIncludesAsync(appointmentId);
            return MapToDtoFromEntity(appointment);
        }

        private static AppointmentResponseDto MapToDtoFromEntity(Appointment a) => new()
        {
            Id = a.Id,
            ClientId = a.ClientId,
            ClientFullName = a.Client.FullName,
            ProviderId = a.ProviderId,
            ProviderBusinessName = a.Provider.BusinessName,
            ServiceId = a.ServiceId,
            ServiceName = a.Service.Name,
            StartDateTime = a.StartDateTime,
            EndDateTime = a.EndDateTime,
            Status = a.Status.ToString(),
            Notes = a.Notes
        };
    }
}
