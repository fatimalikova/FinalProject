using AppointmentAPP.Dtos.SliderDtos;

namespace AppointmentAPP.Interfaces
{
    public interface ISliderService
    {
        Task<List<ResponseSliderDto>> GetActiveAsync();
        Task<List<ResponseSliderDto>> GetAllAsync();
        Task<ResponseSliderDto> CreateAsync(CreateSliderDto dto);
        Task<ResponseSliderDto> UpdateAsync(Guid id, UpdateSliderDto dto);
        Task DeleteAsync(Guid id);
    }
}
