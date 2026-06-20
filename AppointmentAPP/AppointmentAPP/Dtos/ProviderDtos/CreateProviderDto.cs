using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.ProviderDtos
{
    public class CreateProviderDto
    {
        public string BusinessName { get; set; }
        public string Category { get; set; }
        public string? Description { get; set; }
        public string Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }


    public class CreateProviderValidator : AbstractValidator<CreateProviderDto>
    {
        public CreateProviderValidator()
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

            RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300).WithMessage("Address cannot exceed 300 characters.");

            RuleFor(x => x.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Invalid latitude.")
                .When(x => x.Latitude.HasValue);

            RuleFor(x => x.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Invalid longitude.")
                .When(x => x.Longitude.HasValue);
        }
    }
}
