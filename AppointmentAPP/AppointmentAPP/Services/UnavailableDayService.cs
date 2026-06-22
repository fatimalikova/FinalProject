using AppointmentAPP.Data;
using AppointmentAPP.Dtos.UnavailableDayDtos;
using AppointmentAPP.Enums;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class UnavailableDayService(AppDbContext db) : IUnavailableDayService
    {
        public async Task<ResponseUnavailableDayDto> CreateAsync(Guid userId, CreateUnavailableDayDto dto)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            // 🔑 Konflikt yoxlanışı — həmin gündə artıq Confirmed appointment varmı?
            var conflictCount = await db.Appointments.CountAsync(a =>
                a.ProviderId == provider.Id &&
                a.Status == AppointmentStatus.Confirmed &&
                a.StartDateTime.Date == dto.Date.Date);

            if (conflictCount > 0)
                throw new BadRequestException(
                    $"Cannot mark this day as unavailable: there are {conflictCount} confirmed appointment(s) " +
                    $"on this date. Cancel or reschedule them first.");

            var alreadyExists = await db.UnavailableDays.AnyAsync(u =>
                u.ProviderId == provider.Id && u.Date.Date == dto.Date.Date);

            if (alreadyExists)
                throw new BadRequestException("This date is already marked as unavailable.");

            var unavailableDay = new UnavailableDay
            {
                ProviderId = provider.Id,
                Date = dto.Date.Date,
                Reason = dto.Reason
            };

            db.UnavailableDays.Add(unavailableDay);
            await db.SaveChangesAsync();

            return MapToDto(unavailableDay);
        }

        public async Task<List<ResponseUnavailableDayDto>> GetByProviderAsync(Guid providerId)
        {
            var days = await db.UnavailableDays
                .Where(u => u.ProviderId == providerId && u.Date >= DateTime.UtcNow.Date)
                .OrderBy(u => u.Date)
                .ToListAsync();

            return days.Select(MapToDto).ToList();
        }

        public async Task DeleteAsync(Guid userId, Guid unavailableDayId)
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.UserId == userId)
                ?? throw new NotFoundException("Provider profile not found.");

            var day = await db.UnavailableDays
                .FirstOrDefaultAsync(u => u.Id == unavailableDayId && u.ProviderId == provider.Id)
                ?? throw new NotFoundException("Unavailable day entry not found.");

            db.UnavailableDays.Remove(day);
            await db.SaveChangesAsync();
        }

        private static ResponseUnavailableDayDto MapToDto(UnavailableDay u) => new()
        {
            Id = u.Id,
            Date = u.Date,
            Reason = u.Reason
        };
    }
}