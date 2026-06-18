using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class UnavailableDay : BaseEntity
    {
        public DateTime Date { get; set; }
        public string? Reason { get; set; }

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; }
    }
}
