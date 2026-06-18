using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Provider : BaseEntity
    {
        public string BusinessName { get; set; }

        public string Description { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; }

        public List<Service> Services { get; set; }
            = new List<Service>();

        public List<WorkingHour> WorkingHours { get; set; }
            = new List<WorkingHour>();

        public List<Appointment> Appointments { get; set; }
            = new List<Appointment>();

        public List<Review> Reviews { get; set; }
            = new List<Review>();
    }
}
