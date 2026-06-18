namespace AppointmentAPP.Dtos.UnavailableDayDtos
{
    public class ResponseUnavailableDayDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string? Reason { get; set; }
    }
}
