using AppointmentAPP.Dtos.SystemSetting;

namespace AppointmentAPP.Interfaces
{
    public interface ISystemSettingService
    {
        Task<ResponseSystemSettingDto> GetAsync();
        Task<ResponseSystemSettingDto> UpdateAsync(UpdateSystemSettingDto dto);
    }
}