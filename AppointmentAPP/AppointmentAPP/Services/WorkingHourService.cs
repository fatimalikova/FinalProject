using AppointmentAPP.Data;
using AppointmentAPP.Dtos.WorkingHourDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class WorkingHourService(AppDbContext db) : IWorkingHourService
    {
        public async Task<List<ResponseWorkingHourDto>> SetWorkingHoursAsync(Guid userId, List<CreateWorkingHourDto> dtos)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            if (dtos.GroupBy(d => d.Day).Any(g => g.Count() > 1))
                throw new BadRequestException("Each day can only have one working hour entry.");

            foreach (var d in dtos)
            {
                if (d.StartTime >= d.EndTime)
                    throw new BadRequestException($"Invalid hours for {d.Day}: start must be before end.");
            }

            //Konflikt yoxlanışı — gələcək Confirmed appointment-lər yeni saatlara sığırmı?
            var now = DateTime.UtcNow;
            var futureConfirmed = await db.Appointments
                .Where(a => a.ProviderId == provider.Id
                            && a.Status == AppointmentStatus.Confirmed
                            && a.StartDateTime > now)
                .ToListAsync();

            foreach (var appt in futureConfirmed)
            {
                var day = appt.StartDateTime.DayOfWeek;
                var matchingHour = dtos.FirstOrDefault(d => d.Day == day);

                if (matchingHour is null)
                    throw new BadRequestException(
                        $"Cannot update working hours: you have a confirmed appointment on " +
                        $"{appt.StartDateTime:dd MMM yyyy HH:mm} ({day}), but {day} is not in the new schedule. " +
                        $"Cancel or reschedule it first.");

                var apptStart = TimeOnly.FromDateTime(appt.StartDateTime);
                var apptEnd = TimeOnly.FromDateTime(appt.EndDateTime);

                if (apptStart < matchingHour.StartTime || apptEnd > matchingHour.EndTime)
                    throw new BadRequestException(
                        $"Cannot update working hours: a confirmed appointment on " +
                        $"{appt.StartDateTime:dd MMM yyyy HH:mm} falls outside the new {day} hours. " +
                        $"Cancel or reschedule it first.");
            }

            var existing = db.WorkingHours.Where(w => w.ProviderId == provider.Id);
            db.WorkingHours.RemoveRange(existing);

            var newEntries = dtos.Select(d => new WorkingHour
            {
                ProviderId = provider.Id,
                Day = d.Day,
                StartTime = d.StartTime,
                EndTime = d.EndTime
            }).ToList();

            db.WorkingHours.AddRange(newEntries);
            await db.SaveChangesAsync();

            return newEntries.Select(MapToDto).ToList();
        }

        public async Task<List<ResponseWorkingHourDto>> GetByProviderAsync(Guid providerId)
        {
            var realProviderId = await db.Providers
            .Where(p => p.Id == providerId || p.UserId == providerId)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

            if (realProviderId == Guid.Empty)
                return new List<ResponseWorkingHourDto>();

            var hours = await db.WorkingHours
                .Where(w => w.ProviderId == realProviderId)
                .OrderBy(w => w.Day)
                .ToListAsync();

            return hours.Select(MapToDto).ToList();
        }

        public async Task DeleteAsync(Guid userId, Guid workingHourId)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            var hour = await db.WorkingHours
                .FirstOrDefaultAsync(w => w.Id == workingHourId && w.ProviderId == provider.Id)
                ?? throw new NotFoundException("Working hour entry not found.");

            db.WorkingHours.Remove(hour);
            await db.SaveChangesAsync();
        }

        private static ResponseWorkingHourDto MapToDto(WorkingHour w) => new()
        {
            Id = w.Id,
            Day = w.Day,
            StartTime = w.StartTime,
            EndTime = w.EndTime
        };
    }
}
