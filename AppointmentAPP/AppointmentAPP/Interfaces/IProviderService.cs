using AppointmentAPP.Dtos.ProviderDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IProviderService
    {
        Task<ResponseProviderDto> RegisterAsync(Guid userId, CreateProviderDto dto);
        Task<ResponseProviderDto> GetMyProfileAsync(Guid userId);
        Task<ResponseProviderDto> GetByIdAsync(Guid providerId);
        Task<List<ResponseProviderDto>> GetAllAsync(string? category, int page, int pageSize);
        Task<List<ResponseProviderDto>> GetAllForAdminAsync(string? status, int page, int pageSize);
        Task<ResponseProviderDto> UpdateAsync(Guid userId, UpdateProviderDto dto);
        Task ApproveAsync(Guid providerId);
        Task RejectAsync(Guid providerId);
        Task<ProviderDashboardDto> GetMyDashboardAsync(Guid userId);
    }
}
