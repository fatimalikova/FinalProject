using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.PostDtos
{
    public class CreateCommentDto
    {
        //[Required, MaxLength(1000)]
        public string Content { get; set; }
    }


    public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
    {
        public CreateCommentDtoValidator()
        {
            RuleFor(c => c.Content)
                .NotEmpty().WithMessage("Content can not be empty.")
                .MaximumLength(1000).WithMessage("Content cannot exceed 1000 characters.");
        }
    }
}
