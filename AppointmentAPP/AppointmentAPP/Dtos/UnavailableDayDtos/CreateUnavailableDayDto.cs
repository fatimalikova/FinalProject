using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.UnavailableDayDtos
{
    public class CreateUnavailableDayDto
    {
        public DateTime Date { get; set; }
        public string? Reason { get; set; }
    }


    public class CreateUnavailableDayValidator : AbstractValidator<CreateUnavailableDayDto>
    {
        public CreateUnavailableDayValidator()
        {
            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required.")
                .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Date cannot be in the past.");

            RuleFor(x => x.Reason)
                .MaximumLength(300).WithMessage("Reason cannot exceed 300 characters.")
                .When(x => x.Reason != null);
        }
    }
}
