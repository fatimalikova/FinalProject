namespace AppointmentAPP.Dtos.ServiceDtos
{
    public class ServiceCatalogItemDto
    {
        public string Name { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public int ProviderCount { get; set; }
    }
}
