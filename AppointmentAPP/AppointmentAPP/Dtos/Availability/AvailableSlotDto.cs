namespace AppointmentAPP.Dtos.Availability
{
    public class AvailableSlotDto
    {
        public DateTime Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
       // public bool IsAvailable { get; set; }

    }
}
