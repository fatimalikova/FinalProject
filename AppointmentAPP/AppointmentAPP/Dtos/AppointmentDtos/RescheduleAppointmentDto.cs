using AppointmentAPP.Dtos.ProviderDtos;
using AppointmentAPP.Dtos.ServiceDtos;
using AppointmentAPP.Dtos.UserDtos;
using AppointmentAPP.Enums;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.AppointmentDtos
{
    public class RescheduleAppointmentDto
    {
        public DateTime NewStartDateTime { get; set; }
    }



    public class RescheduleAppointmentValidator : AbstractValidator<RescheduleAppointmentDto>
    {
        public RescheduleAppointmentValidator()
        {
            RuleFor(x => x.NewStartDateTime)
                .NotEmpty().WithMessage("New start date/time is required.")
                .GreaterThan(DateTime.UtcNow).WithMessage("New appointment time must be in the future.");
        }
    }
}
