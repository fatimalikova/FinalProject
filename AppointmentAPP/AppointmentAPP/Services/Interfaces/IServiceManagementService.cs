using AppointmentAPP.Dtos.ServiceDtos;

namespace AppointmentAPP.Services.Interfaces
{
    public interface IServiceManagementService
    {
        Task<ResponseServiceDto> CreateAsync(Guid userId, CreateServiceDto dto);
        Task<ResponseServiceDto> UpdateAsync(Guid userId, Guid serviceId, UpdateServiceDto dto);
        Task DeleteAsync(Guid userId, Guid serviceId);
        Task<List<ResponseServiceDto>> GetByProviderAsync(Guid providerId);
        Task<List<ResponseServiceDto>> GetMyServicesAsync(Guid userId);
    }
}
