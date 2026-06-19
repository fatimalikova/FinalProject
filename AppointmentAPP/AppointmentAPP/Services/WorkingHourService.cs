using AppointmentAPP.Data;
using AppointmentAPP.Dtos.WorkingHourDtos;
using AppointmentAPP.Models;
using AppointmentAPP.Services.Interfaces;
using AppointmentAPP.Exceptions;
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
            var hours = await db.WorkingHours
                .Where(w => w.ProviderId == providerId)
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
