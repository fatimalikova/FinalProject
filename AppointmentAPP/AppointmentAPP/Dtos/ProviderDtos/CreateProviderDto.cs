using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.ProviderDtos
{
    public class CreateProviderDto
    {
        [Required, MaxLength(150)]
        public string BusinessName { get; set; }

        [Required, MaxLength(80)]
        public string Category { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
    }
}
