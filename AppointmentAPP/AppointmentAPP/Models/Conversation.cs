using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Conversation : BaseEntity
    {
        public Guid ClientId { get; set; }
        public AppUser Client { get; set; }

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; }

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public List<Message> Messages { get; set; } = new();
    }
}
