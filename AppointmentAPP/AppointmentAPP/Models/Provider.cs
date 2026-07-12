using AppointmentAPP.Enums;
using AppointmentAPP.Models.Entity;

namespace AppointmentAPP.Models
{
    public class Provider : BaseEntity
    {
        public string BusinessName { get; set; }
        public string? ImageUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }          
        public ProviderStatus Status { get; set; } = ProviderStatus.Pending;
        public bool IsActive { get; set; } = true;

        public string Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ContactEmail { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }

        public Guid UserId { get; set; }
        public AppUser User { get; set; }

        public List<ProviderPost> Posts { get; set; } = new();
        public List<Follow> Followers { get; set; } = new();

        public List<Service> Services { get; set; } = new();
        public List<WorkingHour> WorkingHours { get; set; } = new();
        public List<UnavailableDay> UnavailableDays { get; set; } = new();
        public List<Appointment> Appointments { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
    }
}
