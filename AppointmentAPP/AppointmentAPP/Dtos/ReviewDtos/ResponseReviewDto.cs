using AppointmentAPP.Dtos.UserDtos;

namespace AppointmentAPP.Dtos.ReviewDtos
{
    public class ResponseReviewDto
    {
        public Guid Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string? ClientImageUrl { get; set; }
        public string ClientFullName { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
