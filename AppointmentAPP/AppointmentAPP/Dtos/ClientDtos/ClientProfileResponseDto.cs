namespace AppointmentAPP.Dtos.ClientDtos
{
    public class ClientProfileResponseDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UpcomingAppointmentsCount { get; set; }
        public int PastAppointmentsCount { get; set; }
        public int ReviewsWrittenCount { get; set; }
    }
}
