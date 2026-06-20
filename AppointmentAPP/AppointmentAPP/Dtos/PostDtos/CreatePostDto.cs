using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.PostDtos
{
    public class CreatePostDto
    {
        public string? Caption { get; set; }
        public string ImageUrl { get; set; }
    }

    public class CreatePostDtoValidator : AbstractValidator<CreatePostDto>
    {
        public CreatePostDtoValidator()
        {
            RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("Image is required.")
            .MaximumLength(1000);

            RuleFor(x => x.Caption)
                .MaximumLength(500)
                .When(x => x.Caption != null);
        }
    }
}
