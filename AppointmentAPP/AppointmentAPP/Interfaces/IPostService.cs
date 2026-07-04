using AppointmentAPP.Dtos.PostDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IPostService
    {
        Task<ResponsePostDto> CreateAsync(Guid userId, CreatePostDto dto);
        Task DeleteAsync(Guid userId, Guid postId);
        Task<List<ResponsePostDto>> GetByProviderAsync(Guid providerId, Guid? currentUserId);

        Task LikeAsync(Guid userId, Guid postId);
        Task UnlikeAsync(Guid userId, Guid postId);

        Task<ResponseCommentDto> AddCommentAsync(Guid userId, Guid postId, CreateCommentDto dto);
        Task<List<ResponseCommentDto>> GetCommentsAsync(Guid postId);
        Task DeleteCommentAsync(Guid requestingUserId, Guid commentId, bool isPostOwner = false);
        Task<ResponseCommentDto> UpdateCommentAsync(Guid userId, Guid commentId, string content);
    }
}
