using AppointmentAPP.Dtos.ServiceDtos;
using AppointmentAPP.Dtos.UserDtos;
using AppointmentAPP.Dtos.WorkingHourDtos;

namespace AppointmentAPP.Dtos.ProviderDtos
{
    public class ResponseProviderDto
    {
        public Guid Id { get; set; }
        public string? ImageUrl { get; set; }
        public string BusinessName { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string OwnerFullName { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
