using AppointmentAPP.Enums;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.AppointmentDtos
{
    public class CancelAppointmentDto
    {
        public string? Reason { get; set; }
    }


    public class CancelAppointmentValidator : AbstractValidator<CancelAppointmentDto>
    {
        public CancelAppointmentValidator()
        {
            RuleFor(x => x.Reason)
                .MaximumLength(300).WithMessage("Reason cannot exceed 300 characters.")
                .When(x => x.Reason != null);
        }
    }
}
