using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.ReviewDtos
{
    public class CreateReviewDto
    {
        [Required]
        public Guid ProviderId { get; set; }

        [Required]
        public Guid AppointmentId { get; set; } // yalnız Completed appointment üçün review yazıla bilsin

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }
}
