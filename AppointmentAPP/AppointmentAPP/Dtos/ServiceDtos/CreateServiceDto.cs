using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.ServiceDtos
{
    public class CreateServiceDto
    {
        [Required, MaxLength(120)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required, Range(0, 100000)]
        public decimal Price { get; set; }

        [Required, Range(5, 480)]
        public int DurationMinutes { get; set; }
    }
}
