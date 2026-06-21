using AppointmentAPP.Dtos.MessageDtos;
using AppointmentAPP.Extensions;
using AppointmentAPP.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AppointmentAPP.Hubs
{
    [Authorize]
    public class ChatHub(IMessageService messageService, IValidator<SendMessageDto> validator) : Hub
    {
        public async Task SendMessage(SendMessageDto dto)
        {
            var validation = validator.Validate(dto);
            if (!validation.IsValid)
            {
                await Clients.Caller.SendAsync("MessageError",
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                return;
            }

            try
            {
                var senderId = Context.User!.GetUserId();
                var message = await messageService.SendAsync(senderId, dto);

                // qarşı tərəfə canlı çatdırır (online-dırsa)
                await Clients.User(message.ReceiverId.ToString()).SendAsync("ReceiveMessage", message);

                // göndərənin öz UI-ına təsdiq
                await Clients.Caller.SendAsync("MessageSent", message);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("MessageError", ex.Message);
            }
        }
    }
}