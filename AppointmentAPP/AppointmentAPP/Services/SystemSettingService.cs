using AppointmentAPP.Data;
using AppointmentAPP.Dtos.SystemSetting;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.EntityFrameworkCore;

namespace AppointmentAPP.Services
{
    public class SystemSettingService(AppDbContext db) : ISystemSettingService
    {
        public async Task<ResponseSystemSettingDto> GetAsync()
        {
            var setting = await GetOrCreateAsync();
            return MapToDto(setting);
        }

        public async Task<ResponseSystemSettingDto> UpdateAsync(UpdateSystemSettingDto dto)
        {
            var setting = await GetOrCreateAsync();

            setting.MinCancellationNoticeHours = dto.MinCancellationNoticeHours;
            setting.MaxAdvanceBookingDays = dto.MaxAdvanceBookingDays;
            setting.DefaultSlotIntervalMinutes = dto.DefaultSlotIntervalMinutes;
            setting.ReminderHoursBeforeAppointment = dto.ReminderHoursBeforeAppointment;
            setting.RequireProviderApproval = dto.RequireProviderApproval;
            setting.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return MapToDto(setting);
        }

        private async Task<SystemSetting> GetOrCreateAsync()
        {
            var setting = await db.SystemSettings.FirstOrDefaultAsync();
            if (setting is null)
            {
                setting = new SystemSetting();
                db.SystemSettings.Add(setting);
                await db.SaveChangesAsync();
            }
            return setting;
        }

        private static ResponseSystemSettingDto MapToDto(SystemSetting s) => new()
        {
            MinCancellationNoticeHours = s.MinCancellationNoticeHours,
            MaxAdvanceBookingDays = s.MaxAdvanceBookingDays,
            DefaultSlotIntervalMinutes = s.DefaultSlotIntervalMinutes,
            ReminderHoursBeforeAppointment = s.ReminderHoursBeforeAppointment,
            RequireProviderApproval = s.RequireProviderApproval
        };

        public async Task ResetAsync()
        {
            var settings = await db.SystemSettings.FirstOrDefaultAsync();
            if (settings != null)
            {
                settings.MinCancellationNoticeHours = 0;
                settings.MaxAdvanceBookingDays = 0;
                settings.DefaultSlotIntervalMinutes = 0;
                settings.ReminderHoursBeforeAppointment = 0;
                settings.RequireProviderApproval = false;
                await db.SaveChangesAsync();
            }
        }
    }
}