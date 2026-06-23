using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Service : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; } = true;

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; }
        public bool DeactivatedByAdmin { get; set; } = false;

        public List<Appointment> Appointments { get; set; } = new();
    }
}
