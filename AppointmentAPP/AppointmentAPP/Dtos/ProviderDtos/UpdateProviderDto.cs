using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.ProviderDtos
{
    public class UpdateProviderDto
    {
        public string BusinessName { get; set; }
        public string Category { get; set; }
        public string? Description { get; set; }
    }




    public class UpdateProviderValidator : AbstractValidator<UpdateProviderDto>
    {
        public UpdateProviderValidator()
        {
            RuleFor(x => x.BusinessName)
                .NotEmpty().WithMessage("Business name is required.")
                .MaximumLength(150).WithMessage("Business name cannot exceed 150 characters.");

            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Category is required.")
                .MaximumLength(80).WithMessage("Category cannot exceed 80 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.")
                .When(x => x.Description != null);
        }
    }
}
