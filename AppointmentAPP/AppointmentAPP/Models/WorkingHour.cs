using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class WorkingHour : BaseEntity
    {
        public DayOfWeek Day { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public Guid ProviderId { get; set; }

        public Provider Provider { get; set; }
    }
}
