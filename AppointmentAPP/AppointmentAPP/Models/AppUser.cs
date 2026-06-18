using AppointmentAPP.Models.Entity;
using Microsoft.AspNetCore.Identity;
using System.Data;

namespace AppointmentAPP.Models
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FullName { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }   
        public string? TwoFactorCode { get; set; }
        public DateTime? TwoFactorCodeExpiry { get; set; }

        public string? ProfileImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Provider? Provider { get; set; }
        public List<Appointment> Appointments { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
    }
}
