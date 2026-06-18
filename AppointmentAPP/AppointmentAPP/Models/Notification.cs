using AppointmentAPP.Enums;
using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Notification : BaseEntity
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;

        public Guid UserId { get; set; }
        public AppUser User { get; set; }
    }
}
