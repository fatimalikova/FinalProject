using AppointmentAPP.Dtos.ReviewDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IReviewService
    {
        Task<ResponseReviewDto> CreateAsync(Guid clientId, CreateReviewDto dto);
        Task<List<ResponseReviewDto>> GetByProviderAsync(Guid providerId);
        Task DeleteAsync(Guid userId, Guid reviewId);
    }
}
