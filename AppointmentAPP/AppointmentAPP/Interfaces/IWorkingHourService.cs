using AppointmentAPP.Dtos.WorkingHourDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IWorkingHourService
    {
        Task<List<ResponseWorkingHourDto>> SetWorkingHoursAsync(Guid userId, List<CreateWorkingHourDto> dtos);
        Task<List<ResponseWorkingHourDto>> GetByProviderAsync(Guid providerId);
        Task DeleteAsync(Guid userId, Guid workingHourId);
    }
}
