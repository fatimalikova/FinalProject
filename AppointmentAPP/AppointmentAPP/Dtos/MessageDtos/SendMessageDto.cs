using FluentValidation;

namespace AppointmentAPP.Dtos.MessageDtos
{
    public class SendMessageDto
    {
        public Guid? ConversationId { get; set; }

        // Client ilk dəfə provider-ə yazırsa (yeni conversation yaranır)
        public Guid? ProviderId { get; set; }

        public string Content { get; set; }
    }


    public class SendMessageValidator : AbstractValidator<SendMessageDto>
    {
        public SendMessageValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Message cannot be empty.")
                .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters.");

            RuleFor(x => x)
                .Must(x => x.ConversationId.HasValue || x.ProviderId.HasValue)
                .WithMessage("Either ConversationId or ProviderId must be provided.");
        }
    }
}
