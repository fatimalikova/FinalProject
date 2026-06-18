using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.AppointmentDtos
{
    public class BookAppointmentDto
    {
        [Required]
        public Guid ProviderId { get; set; }

        [Required]
        public Guid ServiceId { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
