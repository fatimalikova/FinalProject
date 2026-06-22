using AppointmentAPP.Dtos.MessageDtos;

namespace AppointmentAPP.Interfaces
{
    public interface IMessageService
    {
        Task<ResponseMessageDto> SendAsync(Guid senderId, SendMessageDto dto);
        Task<List<ResponseConversationDto>> GetMyConversationsAsync(Guid userId);
        Task<List<ResponseMessageDto>> GetConversationMessagesAsync(Guid userId, Guid conversationId);
        Task MarkAsReadAsync(Guid userId, Guid conversationId);
    }
}
