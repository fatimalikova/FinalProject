using AppointmentAPP.Dtos.SystemSetting;

namespace AppointmentAPP.Services.Interfaces
{
    public interface ISystemSettingService
    {
        Task<ResponseSystemSettingDto> GetAsync();
        Task<ResponseSystemSettingDto> UpdateAsync(UpdateSystemSettingDto dto);
    }
}