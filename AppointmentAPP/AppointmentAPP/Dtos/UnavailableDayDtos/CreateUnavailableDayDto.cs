using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.UnavailableDayDtos
{
    public class CreateUnavailableDayDto
    {
        [Required]
        public DateTime Date { get; set; }

        [MaxLength(300)]
        public string? Reason { get; set; }
    }
}
