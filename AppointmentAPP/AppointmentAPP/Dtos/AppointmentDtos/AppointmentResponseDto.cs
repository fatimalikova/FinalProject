namespace AppointmentAPP.Dtos.AppointmentDtos
{
    public class AppointmentResponseDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }
        public string ClientFullName { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderBusinessName { get; set; }
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Status { get; set; }
        public string? Notes { get; set; }
    }
}
