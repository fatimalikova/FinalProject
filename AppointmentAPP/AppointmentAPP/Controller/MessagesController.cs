using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessagesController(IMessageService messageService) : BaseController
    {
        [HttpGet("conversations")]
        public async Task<IActionResult> GetMyConversations()
        {
            var result = await messageService.GetMyConversationsAsync(User.GetUserId());
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [HttpGet("conversations/{id:guid}")]
        public async Task<IActionResult> GetMessages(Guid id)
        {
            var result = await messageService.GetConversationMessagesAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [HttpPut("conversations/{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await messageService.MarkAsReadAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Marked as read."));
        }
    }
}
