using AppointmentAPP.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.AppointmentDtos
{
    public class CancelAppointmentDto
    {
        [MaxLength(300)]
        public string? Reason { get; set; }
    }
}
