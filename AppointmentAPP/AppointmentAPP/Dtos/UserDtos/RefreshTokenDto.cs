using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.UserDtos
{
    public class RefreshTokenDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }


    public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
    {
        public RefreshTokenDtoValidator()
        {
            RuleFor(x => x.AccessToken)
                .NotEmpty().WithMessage("Access token is required.");

            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("Refresh token is required.");
        }
    }
}
