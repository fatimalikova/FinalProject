using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace AppointmentAPP.Dtos.ServiceDtos
{
    public class CreateServiceDto
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
    }




    public class CreateServiceValidator : AbstractValidator<CreateServiceDto>
    {
        public CreateServiceValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Service name is required.")
                .MaximumLength(120).WithMessage("Name cannot exceed 120 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
                .When(x => x.Description != null);

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price must be 0 or greater.");

            RuleFor(x => x.DurationMinutes)
                .InclusiveBetween(5, 480).WithMessage("Duration must be between 5 and 480 minutes.");
        }
    }
}
