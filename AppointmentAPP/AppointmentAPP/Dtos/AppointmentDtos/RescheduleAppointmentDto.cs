using AppointmentAPP.Dtos.ProviderDtos;
using AppointmentAPP.Dtos.ServiceDtos;
using AppointmentAPP.Dtos.UserDtos;
using AppointmentAPP.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.AppointmentDtos
{
    public class RescheduleAppointmentDto
    {
        [Required]
        public DateTime NewStartDateTime { get; set; }
    }
}
