using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.ReviewDtos
{
    public class CreateReviewDto
    {
        public Guid ProviderId { get; set; }

        public Guid AppointmentId { get; set; } // yalnız Completed appointment üçün review yazıla bilsin

        public int Rating { get; set; }

        public string? Comment { get; set; }
    }



    public class CreateReviewValidator : AbstractValidator<CreateReviewDto>
    {
        public CreateReviewValidator()
        {
            RuleFor(x => x.ProviderId)
                .NotEmpty().WithMessage("ProviderId is required.");

            RuleFor(x => x.AppointmentId)
                .NotEmpty().WithMessage("AppointmentId is required.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

            RuleFor(x => x.Comment)
                .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.")
                .When(x => x.Comment != null);
        }
    }
}
