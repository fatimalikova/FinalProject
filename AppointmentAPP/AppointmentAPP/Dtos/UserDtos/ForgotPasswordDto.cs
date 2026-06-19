using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.UserDtos
{
    public class ForgotPasswordDto
    {
        public string Email { get; set; }
    }


    public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
        }
    }

}
