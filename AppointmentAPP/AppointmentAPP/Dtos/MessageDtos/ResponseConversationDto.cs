namespace AppointmentAPP.Dtos.MessageDtos
{
    public class ResponseConversationDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string ClientFullName { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderBusinessName { get; set; }
        public string? LastMessageContent { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
