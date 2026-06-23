using AppointmentAPP.Dtos.ServiceDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IServiceManagementService
    {
        Task<ResponseServiceDto> CreateAsync(Guid userId, CreateServiceDto dto);
        Task<ResponseServiceDto> UpdateAsync(Guid userId, Guid serviceId, UpdateServiceDto dto);
        Task DeleteAsync(Guid userId, Guid serviceId);
        Task<List<ResponseServiceDto>> GetByProviderAsync(Guid providerId);
        Task<List<ResponseServiceDto>> GetMyServicesAsync(Guid userId);
        Task<List<ResponseServiceDto>> GetAllForAdminAsync(Guid? providerId, bool? isActive);
        Task DeactivateByAdminAsync(Guid serviceId);
        Task<List<ServiceCatalogItemDto>> GetCatalogAsync();
        Task<List<ServiceSearchResultDto>> SearchByNameAsync(string name);
        Task ReactivateByAdminAsync(Guid serviceId);
    }
}
