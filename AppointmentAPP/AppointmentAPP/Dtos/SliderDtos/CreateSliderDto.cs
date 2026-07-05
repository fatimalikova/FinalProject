namespace AppointmentAPP.Dtos.SliderDtos
{
    public class CreateSliderDto
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ButtonText { get; set; }
        public string? ButtonLink { get; set; }
        public int Order { get; set; } = 0;
    }
}
