using AppointmentAPP.Data;
using AppointmentAPP.Dtos.AppointmentDtos;
using AppointmentAPP.Dtos.ClientDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class AppointmentService(
        AppDbContext db,
        IAvailabilityService availabilityService,
        INotificationService notificationService,
        IEmailService emailService,
        UserManager<AppUser> userManager
    ) : IAppointmentService
    {
        public async Task<ResponseAppointmentDto> BookAsync(Guid clientId, BookAppointmentDto dto)
        {
            var provider = await db.Providers
                .FirstOrDefaultAsync(p => p.Id == dto.ProviderId || p.UserId == dto.ProviderId)
                ?? throw new NotFoundException("Provider not found.");

            var service = await db.Services
                .FirstOrDefaultAsync(s => s.Id == dto.ServiceId
                                        && s.IsActive
                                        && s.ProviderId == provider.Id)
                ?? throw new NotFoundException("Service not found for this provider.");

            if (provider.Status != ProviderStatus.Approved || !provider.IsActive)
                throw new BadRequestException("This provider is not currently accepting bookings.");

            var endDateTime = dto.StartDateTime.AddMinutes(service.DurationMinutes);

            var setting = await db.SystemSettings.FirstOrDefaultAsync();
            var maxAdvanceDays = setting?.MaxAdvanceBookingDays ?? 30;

            if (dto.StartDateTime > DateTime.UtcNow.AddDays(maxAdvanceDays))
                throw new BadRequestException($"Bookings can only be made up to {maxAdvanceDays} days in advance.");

            if (dto.StartDateTime < DateTime.UtcNow)
                throw new BadRequestException("Cannot book an appointment in the past.");

            var clientConflict = await db.Appointments
                .AnyAsync(a =>
                a.ClientId == clientId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.StartDateTime < endDateTime &&
                a.EndDateTime > dto.StartDateTime);

            if (clientConflict)
                throw new BadRequestException("You already have an appointment scheduled for this time slot. Please choose a different time.");

            var isAvailable = await availabilityService.IsSlotAvailableAsync(
                provider.Id, dto.StartDateTime, endDateTime);
            if (!isAvailable)
                throw new BadRequestException("This time slot is no longer available.");

            var appointment = new Appointment
            {
                ClientId = clientId,
                ProviderId = provider.Id,
                ServiceId = dto.ServiceId,
                StartDateTime = dto.StartDateTime,
                EndDateTime = endDateTime,
                Status = AppointmentStatus.Confirmed,
                Notes = dto.Notes,
                PriceAtBooking = service.Price
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            var client = await db.Users.FirstAsync(u => u.Id == clientId);
            var providerUser = await db.Users.FirstAsync(u => u.Id == provider.UserId);

            await notificationService.CreateAsync(
                clientId, "Appointment Confirmed",
                $"Your appointment for {service.Name} on {dto.StartDateTime:dd MMM yyyy HH:mm} is confirmed.",
                NotificationType.AppointmentConfirmed);

            await notificationService.CreateAsync(
                provider.UserId, "New Appointment Booked",
                $"A new appointment has been booked for {service.Name} on {dto.StartDateTime:dd MMM yyyy HH:mm}.",
                NotificationType.AppointmentBooked);

            await TrySendEmailAsync(
                providerUser.Email!,
                "New Reservation Received",
                $"<h3>New Reservation Received</h3>" +
                $"<p><b>{client.FullName}</b> has booked an appointment for <b>{service.Name}</b>.</p>" +
                $"<p>Date: <b>{dto.StartDateTime:dd MMM yyyy HH:mm}</b></p>" +
                $"<p>Client Email: {client.Email}</p>");

            var admins = await userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                await TrySendEmailAsync(
                    admin.Email!,
                    "New Reservation on Platform",
                    $"<h3>New Reservation Received</h3>" +
                    $"<p>Client: <b>{client.FullName}</b></p>" +
                    $"<p>Provider: <b>{provider.BusinessName}</b></p>" +
                    $"<p>Service: {service.Name}</p>" +
                    $"<p>Date: <b>{dto.StartDateTime:dd MMM yyyy HH:mm}</b></p>");
            }

            await TrySendEmailAsync(
                client.Email!,
                "Reservation Confirmed",
                $"<h3>Your Reservation Confirmed</h3>" +
                $"<p>Dear <b>{client.FullName}</b>,</p>" +
                $"<p>Your appointment for <b>{service.Name}</b> with <b>{provider.BusinessName}</b> has been confirmed.</p>" +
                $"<p>Date: <b>{dto.StartDateTime:dd MMM yyyy HH:mm}</b></p>" +
                $"<p>Address: {provider.Address}</p>" +
                (provider.Latitude.HasValue
                    ? $"<p><a href='https://www.google.com/maps?q={provider.Latitude},{provider.Longitude}'>View on Map</a></p>"
                    : ""));

            return await MapToDto(appointment.Id);
        }


        public async Task<ResponseAppointmentDto> CancelAsync(Guid userId, Guid appointmentId, CancelAppointmentDto dto)
        {
            var appointment = await GetAppointmentWithIncludesAsync(appointmentId);

            EnsureUserIsParticipant(appointment, userId);

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                throw new BadRequestException("This appointment cannot be cancelled.");

            await EnsureNoticeRespectedAsync(appointment, userId);

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

        public async Task<ResponseAppointmentDto> RescheduleAsync(Guid userId, Guid appointmentId, RescheduleAppointmentDto dto)
        {
            var appointment = await GetAppointmentWithIncludesAsync(appointmentId);

            EnsureUserIsParticipant(appointment, userId);

            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
                throw new BadRequestException("This appointment cannot be rescheduled.");

            await EnsureNoticeRespectedAsync(appointment, userId);

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

        public async Task<ResponseAppointmentDto> CompleteAsync(Guid providerUserId, Guid appointmentId)
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

        public async Task<List<ResponseAppointmentDto>> GetMyAppointmentsAsync(Guid clientUserId, string? filter)
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

      
        public async Task<List<ResponseAppointmentDto>> GetProviderCalendarAsync(Guid providerUserId, DateTime from, DateTime to)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == providerUserId)
                ?? throw new NotFoundException("Provider profile not found.");

            var appointments = await db.Appointments
                .Include(a => a.Client)
                .Include(a => a.Provider)
                .Include(a => a.Service)
                .Where(a => a.ProviderId == provider.Id
                            && a.StartDateTime.Date >= from.Date
                            && a.StartDateTime.Date <= to.Date)
                .OrderBy(a => a.StartDateTime)
                .ToListAsync();

            return appointments.Select(MapToDtoFromEntity).ToList();
        }

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
        public async Task<ResponseAppointmentDto> GetByIdAsync(Guid userId, Guid appointmentId)
        {
            var appointment = await GetAppointmentWithIncludesAsync(appointmentId);

            EnsureUserIsParticipant(appointment, userId);

            return MapToDtoFromEntity(appointment);
        }
   
        private async Task TrySendEmailAsync(string to, string subject, string body)
        {
            try
            {
                await emailService.SendEmailAsync(to, subject, body);
            }
            catch
            {
                // Email göndərilməsə də booking uğurlu qalmalıdır.
                // Real production-da bura logging (ILogger) əlavə olunmalıdır.
            }
        }


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

        private async Task EnsureNoticeRespectedAsync(Appointment appointment , Guid actingUserId)
        {
            if (actingUserId != appointment.ClientId)
                return;
            var setting = await db.SystemSettings.FirstOrDefaultAsync();
            var minNoticeHours = setting?.MinCancellationNoticeHours ?? 24;

            var hoursUntilAppointment = (appointment.StartDateTime - DateTime.UtcNow).TotalHours;

            if (hoursUntilAppointment < minNoticeHours)
                throw new BadRequestException(
                    $"This action requires at least {minNoticeHours} hours notice before the appointment.");
        }

        private async Task<ResponseAppointmentDto> MapToDto(Guid appointmentId)
        {
            var appointment = await GetAppointmentWithIncludesAsync(appointmentId);
            return MapToDtoFromEntity(appointment);
        }

        private static ResponseAppointmentDto MapToDtoFromEntity(Appointment a) => new()
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
            Notes = a.Notes,
            Price = a.PriceAtBooking
        };


        public async Task<ResponseClientProfileDto> GetMyClientProfileAsync(Guid userId)
        {
            var user = await db.Users.FirstAsync(u => u.Id == userId);
            var now = DateTime.UtcNow;

            var upcoming = await db.Appointments.CountAsync(a =>
                a.ClientId == userId && a.StartDateTime >= now && a.Status != AppointmentStatus.Cancelled);

            var past = await db.Appointments.CountAsync(a =>
                a.ClientId == userId && (a.StartDateTime < now || a.Status == AppointmentStatus.Completed));

            var reviewsCount = await db.Reviews.CountAsync(r => r.ClientId == userId);

            return new ResponseClientProfileDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? "",
                Email = user.Email!,
                CreatedAt = user.CreatedAt,
                ImageUrl = user.ImageUrl,
                UpcomingAppointmentsCount = upcoming,
                PastAppointmentsCount = past,
                ReviewsWrittenCount = reviewsCount
            };
        }


        public async Task<List<ResponseAppointmentDto>> GetAllForAdminAsync(string? status, DateTime? from, DateTime? to)
        {
            var query = db.Appointments
                .Include(a => a.Client)
                .Include(a => a.Provider)
                .Include(a => a.Service)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AppointmentStatus>(status, true, out var parsedStatus))
                query = query.Where(a => a.Status == parsedStatus);

            if (from.HasValue)
                query = query.Where(a => a.StartDateTime.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(a => a.StartDateTime.Date <= to.Value.Date);

            var appointments = await query.OrderByDescending(a => a.StartDateTime).ToListAsync();
            return appointments.Select(MapToDtoFromEntity).ToList();
        }
    }
}
