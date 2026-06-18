namespace AppointmentAPP.Dtos.ServiceDtos
{
    public class ResponseServiceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; }
        public Guid ProviderId { get; set; }
        public string ProviderBusinessName { get; set; }
    }
}
