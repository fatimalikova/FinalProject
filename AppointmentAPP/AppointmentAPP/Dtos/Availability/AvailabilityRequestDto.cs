using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.Availability
{
    public class AvailabilityRequestDto
    {
        [Required]
        public Guid ProviderId { get; set; }

        [Required]
        public Guid ServiceId { get; set; }

        [Required]
        public DateTime Date { get; set; } // yalnız gün, saat ignore olunacaq
    }
}
