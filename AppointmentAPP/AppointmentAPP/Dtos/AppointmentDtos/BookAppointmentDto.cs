using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.AppointmentDtos
{
    public class BookAppointmentDto
    {
        public Guid ProviderId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime StartDateTime { get; set; }
        public string? Notes { get; set; }
    }


    public class BookAppointmentValidator : AbstractValidator<BookAppointmentDto>
    {
        public BookAppointmentValidator()
        {
            RuleFor(x => x.ProviderId)
                .NotEmpty().WithMessage("ProviderId is required.");

            RuleFor(x => x.ServiceId)
                .NotEmpty().WithMessage("ServiceId is required.");

            RuleFor(x => x.StartDateTime)
                .NotEmpty().WithMessage("Start date/time is required.")
                .GreaterThan(DateTime.UtcNow).WithMessage("Appointment must be booked for a future time.");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.")
                .When(x => x.Notes != null);
        }
    }
}
