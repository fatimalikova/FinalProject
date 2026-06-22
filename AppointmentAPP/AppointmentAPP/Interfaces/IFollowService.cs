using AppointmentAPP.Dtos.PostDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IFollowService
    {
        Task FollowAsync(Guid userId, Guid providerId);
        Task UnfollowAsync(Guid userId, Guid providerId);
        Task<FollowStatusDto> GetStatusAsync(Guid providerId, Guid? currentUserId);
    }
}
