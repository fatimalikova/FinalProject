namespace AppointmentAPP.Dtos.ServiceDtos
{
    public class ServiceSearchResultDto
    {
        public Guid ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }

        public Guid ProviderId { get; set; }
        public string ProviderBusinessName { get; set; }
        public string ProviderCategory { get; set; }
        public string ProviderAddress { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
