namespace AppointmentAPP.Dtos.MessageDtos
{
    public class ResponseMessageDto
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; }
        public string SenderFullName { get; set; }
        public Guid ReceiverId { get; set; } // Hub-ın kimə "push" edəcəyini bilməsi üçün
        public string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
