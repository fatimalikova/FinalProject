using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.UserDtos
{
    public class Verify2FADto
    {
        public string UserName { get; set; }
        public string Code { get; set; }
    }


    public class Verify2FADtoValidator : AbstractValidator<Verify2FADto>
    {
        public Verify2FADtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Username is required.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Code is required.")
                .Length(6).WithMessage("Code must be 6 digits.");
        }
    }
}
