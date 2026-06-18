using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Review : BaseEntity
    {
        public int Rating { get; set; }       
        public string? Comment { get; set; }

        public Guid ClientId { get; set; }
        public AppUser Client { get; set; }

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; }
    }
}
