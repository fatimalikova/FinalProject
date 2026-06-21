using AppointmentAPP.Dtos.PostDtos;

namespace AppointmentAPP.Services.Interfaces
{
    public interface IFollowService
    {
        Task FollowAsync(Guid userId, Guid providerId);
        Task UnfollowAsync(Guid userId, Guid providerId);
        Task<FollowStatusDto> GetStatusAsync(Guid providerId, Guid? currentUserId);
    }
}
