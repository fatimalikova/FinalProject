namespace AppointmentAPP.Dtos.WorkingHourDtos
{
    public class ResponseWorkingHourDto
    {
        public Guid Id { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}
