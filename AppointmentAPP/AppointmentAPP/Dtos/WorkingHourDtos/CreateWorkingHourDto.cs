using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.WorkingHourDtos
{
    public class CreateWorkingHourDto
    {
        public DayOfWeek Day { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }

    public class CreateWorkingHourValidator : AbstractValidator<CreateWorkingHourDto>
    {
        public CreateWorkingHourValidator()
        {
            RuleFor(x => x.Day)
                .IsInEnum().WithMessage("Invalid day value.");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Start time is required.");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("End time is required.");

            RuleFor(x => x)
                .Must(x => x.StartTime < x.EndTime)
                .WithMessage("Start time must be before end time.");
        }
    }
}
