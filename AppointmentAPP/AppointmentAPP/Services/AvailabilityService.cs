using AppointmentAPP.Data;
using AppointmentAPP.Dtos.Availability;
using AppointmentAPP.Enums;
using AppointmentAPP.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly AppDbContext _db;

        public AvailabilityService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(AvailabilityRequestDto request)
        {
            var date = request.Date.Date;
            var dayOfWeek = date.DayOfWeek;

            // 1) Service-i tap (duration üçün lazımdır)
            var service = await _db.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.ProviderId == request.ProviderId && s.IsActive);

            if (service is null)
                return new List<AvailableSlotDto>();

            // 2) Həmin gün unavailable-dirsə, slot yoxdur
            var isUnavailable = await _db.UnavailableDays
                .AnyAsync(u => u.ProviderId == request.ProviderId && u.Date.Date == date);

            if (isUnavailable)
                return new List<AvailableSlotDto>();

            // 3) O günün working hour-unu tap
            var workingHour = await _db.WorkingHours
                .FirstOrDefaultAsync(w => w.ProviderId == request.ProviderId && w.Day == dayOfWeek);

            if (workingHour is null)
                return new List<AvailableSlotDto>();

            // 4) Həmin günün mövcud (aktiv) appointment-lərini tap
            var existingAppointments = await _db.Appointments
                .Where(a => a.ProviderId == request.ProviderId
                            && a.StartDateTime.Date == date
                            && a.Status != AppointmentStatus.Cancelled)
                .Select(a => new { a.StartDateTime, a.EndDateTime })
                .ToListAsync();

            // 5) Slot-ları generasiya et
            var slots = new List<AvailableSlotDto>();
            var duration = TimeSpan.FromMinutes(service.DurationMinutes);

            var slotStart = date.Add(workingHour.StartTime.ToTimeSpan());
            var workEnd = date.Add(workingHour.EndTime.ToTimeSpan());

            while (slotStart.Add(duration) <= workEnd)
            {
                var slotEnd = slotStart.Add(duration);

                // keçmiş vaxtları göstərmə (bugünkü sorğularda)
                bool isPast = slotStart < DateTime.UtcNow;

                // overlap yoxlanışı: mövcud appointment-lərdən biri bu slot-la kəsişirsə, keç
                bool overlaps = existingAppointments.Any(a =>
                    slotStart < a.EndDateTime && slotEnd > a.StartDateTime);

                if (!isPast && !overlaps)
                {
                    slots.Add(new AvailableSlotDto
                    {
                        Date = date,
                        StartTime = TimeOnly.FromTimeSpan(slotStart.TimeOfDay),
                        EndTime = TimeOnly.FromTimeSpan(slotEnd.TimeOfDay)
                    });
                }

                slotStart = slotStart.Add(duration);
            }

            return slots;
        }

        public async Task<bool> IsSlotAvailableAsync(Guid providerId, DateTime startDateTime, DateTime endDateTime, Guid? excludeAppointmentId = null)
        {
            // unavailable day check
            var isUnavailable = await _db.UnavailableDays
                .AnyAsync(u => u.ProviderId == providerId && u.Date.Date == startDateTime.Date);

            if (isUnavailable)
                return false;

            // working hour daxilindədir?
            var workingHour = await _db.WorkingHours
                .FirstOrDefaultAsync(w => w.ProviderId == providerId && w.Day == startDateTime.DayOfWeek);

            if (workingHour is null)
                return false;

            var dayStart = startDateTime.Date.Add(workingHour.StartTime.ToTimeSpan());
            var dayEnd = startDateTime.Date.Add(workingHour.EndTime.ToTimeSpan());

            if (startDateTime < dayStart || endDateTime > dayEnd)
                return false;

            // overlap check (reschedule zamanı özünü exclude etmək üçün excludeAppointmentId)
            var hasOverlap = await _db.Appointments
                .Where(a => a.ProviderId == providerId
                            && a.Status != AppointmentStatus.Cancelled
                            && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
                .AnyAsync(a => startDateTime < a.EndDateTime && endDateTime > a.StartDateTime);

            return !hasOverlap;
        }
    }
}
