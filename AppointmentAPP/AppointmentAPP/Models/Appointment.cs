using AppointmentAPP.Enums;
using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Appointment : BaseEntity
    {
        public Guid ClientId { get; set; }
        public AppUser Client { get; set; }

        public Guid ProviderId { get; set; }
        public Provider Provider { get; set; }

        public Guid ServiceId { get; set; }
        public Service Service { get; set; }

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string? Notes { get; set; }
        public string? CancelReason { get; set; }
        public bool ReminderSent { get; set; } = false;

        public decimal PriceAtBooking { get; set; }
    }
}
