using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController(INotificationService notificationService) : BaseController
    {
        [HttpGet("me")]
        public async Task<IActionResult> GetMy()
        {
            var result = await notificationService.GetMyNotificationsAsync(User.GetUserId());
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await notificationService.MarkAsReadAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Marked as read."));
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await notificationService.MarkAllAsReadAsync(User.GetUserId());
            return Ok(ResponseModelHelper.SuccessResult("All notifications marked as read."));
        }
    }
}