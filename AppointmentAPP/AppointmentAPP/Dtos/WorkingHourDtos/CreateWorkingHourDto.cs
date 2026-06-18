using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.WorkingHourDtos
{
    public class CreateWorkingHourDto
    {
        [Required]
        public DayOfWeek Day { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }
    }
}
