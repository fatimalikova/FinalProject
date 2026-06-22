using AppointmentAPP.Dtos.UnavailableDayDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IUnavailableDayService
    {
        Task<ResponseUnavailableDayDto> CreateAsync(Guid userId, CreateUnavailableDayDto dto);
        Task<List<ResponseUnavailableDayDto>> GetByProviderAsync(Guid providerId);
        Task DeleteAsync(Guid userId, Guid unavailableDayId);
    }
}
