using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.UserDtos
{
    public class EmailConfirmDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
    }

    public class EmailConfirmDtoValidator : AbstractValidator<EmailConfirmDto>
    {
        public EmailConfirmDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("A valid email address is required.");
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token is required.");
        }
    }
}
