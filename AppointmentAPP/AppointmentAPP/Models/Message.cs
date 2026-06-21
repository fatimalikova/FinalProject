using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Message : BaseEntity
    {
        public Guid ConversationId { get; set; }
        public Conversation? Conversation { get; set; }

        public Guid SenderId { get; set; }
        public AppUser? Sender { get; set; }

        public string? Content { get; set; }
        public bool IsRead { get; set; } = false;
    }
}
