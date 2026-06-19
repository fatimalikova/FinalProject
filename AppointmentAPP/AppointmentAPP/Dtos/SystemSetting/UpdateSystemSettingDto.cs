using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.SystemSetting
{
    public class UpdateSystemSettingDto
    {
        public int MinCancellationNoticeHours { get; set; }
        public int MaxAdvanceBookingDays { get; set; }
        public int DefaultSlotIntervalMinutes { get; set; }
        public int ReminderHoursBeforeAppointment { get; set; }

        public bool RequireProviderApproval { get; set; }
    }


    public class UpdateSystemSettingValidator : AbstractValidator<UpdateSystemSettingDto>
    {
        public UpdateSystemSettingValidator()
        {
            RuleFor(x => x.MinCancellationNoticeHours)
                .InclusiveBetween(0, 168).WithMessage("Must be between 0 and 168 hours.");

            RuleFor(x => x.MaxAdvanceBookingDays)
                .InclusiveBetween(1, 365).WithMessage("Must be between 1 and 365 days.");

            RuleFor(x => x.DefaultSlotIntervalMinutes)
                .InclusiveBetween(5, 120).WithMessage("Must be between 5 and 120 minutes.");

            RuleFor(x => x.ReminderHoursBeforeAppointment)
                .InclusiveBetween(1, 168).WithMessage("Must be between 1 and 168 hours.");
        }
    }
}
