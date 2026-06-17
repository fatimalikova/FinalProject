using AppointmentAPP.Models.Entity;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace AppointmentAPP.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }

        public Provider? Provider { get; set; }

        public List<Appointment> Appointments { get; set; }
            = new List<Appointment>();

        public List<Notification> Notifications { get; set; }
            = new List<Notification>();

        public List<Review> Reviews { get; set; }
            = new List<Review>();
    }
}
