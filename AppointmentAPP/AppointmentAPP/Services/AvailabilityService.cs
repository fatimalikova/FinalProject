using AppointmentAPP.Data;
using AppointmentAPP.Dtos.Availability;
using AppointmentAPP.Enums;
using AppointmentAPP.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class AvailabilityService(AppDbContext db) : IAvailabilityService
    {
        public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(AvailabilityRequestDto request)
        {
            var provider = await db.Providers
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId || p.UserId == request.ProviderId);
            if (provider is null) return new List<AvailableSlotDto>();

            var realProviderId = provider.Id;
            var date = request.Date.Date;
            var dayOfWeek = date.DayOfWeek;

            var setting = await db.SystemSettings.FirstOrDefaultAsync();
            var maxAdvanceDays = setting?.MaxAdvanceBookingDays ?? 30;
            if (date > DateTime.UtcNow.Date.AddDays(maxAdvanceDays))
                return new List<AvailableSlotDto>();

            var service = await db.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId
                                        && s.ProviderId == realProviderId
                                        && s.IsActive);
            if (service is null) return new List<AvailableSlotDto>();

            var isUnavailable = await db.UnavailableDays
                .AnyAsync(u => u.ProviderId == realProviderId && u.Date.Date == date);
            if (isUnavailable) return new List<AvailableSlotDto>();

            var workingHour = await db.WorkingHours
                .FirstOrDefaultAsync(w => w.ProviderId == realProviderId && w.Day == dayOfWeek);
            if (workingHour is null) return new List<AvailableSlotDto>();

            var existingAppointments = await db.Appointments
                .Where(a => a.ProviderId == realProviderId
                            && a.StartDateTime.Date == date
                            && a.Status != AppointmentStatus.Cancelled)
                .Select(a => new { a.StartDateTime, a.EndDateTime })
                .ToListAsync();

            var slots = new List<AvailableSlotDto>();
            var duration = TimeSpan.FromMinutes(service.DurationMinutes);

            // step indi DefaultSlotIntervalMinutes-dən gəlir, duration-dan deyil
            var stepMinutes = setting?.DefaultSlotIntervalMinutes ?? 15;
            var step = TimeSpan.FromMinutes(stepMinutes);

            var slotStart = date.Add(workingHour.StartTime.ToTimeSpan());
            var workEnd = date.Add(workingHour.EndTime.ToTimeSpan());

            while (slotStart.Add(duration) <= workEnd)
            {
                var slotEnd = slotStart.Add(duration);
                bool isPast = slotStart < DateTime.UtcNow;
                bool overlaps = existingAppointments.Any(a => slotStart < a.EndDateTime && slotEnd > a.StartDateTime);

                if (!isPast && !overlaps)
                {
                    slots.Add(new AvailableSlotDto
                    {
                        Date = date,
                        StartTime = TimeOnly.FromTimeSpan(slotStart.TimeOfDay),
                        EndTime = TimeOnly.FromTimeSpan(slotEnd.TimeOfDay)
                    });
                }

                slotStart = slotStart.Add(step); // əvvəllər: .Add(duration)
            }

            return slots;
        }

        public async Task<bool> IsSlotAvailableAsync(Guid providerId, DateTime startDateTime, DateTime endDateTime, Guid? excludeAppointmentId = null)
        {
            var provider = await db.Providers
                .FirstOrDefaultAsync(p => p.Id == providerId || p.UserId == providerId);
            if (provider is null) return false;

            var realProviderId = provider.Id;

            var isUnavailable = await db.UnavailableDays
                .AnyAsync(u => u.ProviderId == realProviderId && u.Date.Date == startDateTime.Date);
            if (isUnavailable) return false;

            var workingHour = await db.WorkingHours
                .FirstOrDefaultAsync(w => w.ProviderId == realProviderId && w.Day == startDateTime.DayOfWeek);
            if (workingHour is null) return false;

            var dayStart = startDateTime.Date.Add(workingHour.StartTime.ToTimeSpan());
            var dayEnd = startDateTime.Date.Add(workingHour.EndTime.ToTimeSpan());
            if (startDateTime < dayStart || endDateTime > dayEnd) return false;

            var hasOverlap = await db.Appointments
                .Where(a => a.ProviderId == realProviderId
                            && a.Status != AppointmentStatus.Cancelled
                            && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
                .AnyAsync(a => startDateTime < a.EndDateTime && endDateTime > a.StartDateTime);

            return !hasOverlap;
        }
    }
}