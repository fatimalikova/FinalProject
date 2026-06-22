using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class FollowController(IFollowService followService) : BaseController
    {
        [Authorize(Roles = "Client")]
        [HttpPost("{providerId:guid}")]
        public async Task<IActionResult> Follow(Guid providerId)
        {
            await followService.FollowAsync(User.GetUserId(), providerId);
            return Ok(ResponseModelHelper.SuccessResult("Followed."));
        }

        [Authorize(Roles = "Client")]
        [HttpDelete("{providerId:guid}")]
        public async Task<IActionResult> Unfollow(Guid providerId)
        {
            await followService.UnfollowAsync(User.GetUserId(), providerId);
            return Ok(ResponseModelHelper.SuccessResult("Unfollowed."));
        }

        [AllowAnonymous]
        [HttpGet("{providerId:guid}/status")]
        public async Task<IActionResult> GetStatus(Guid providerId)
        {
            Guid? currentUserId = null;
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (idClaim is not null && Guid.TryParse(idClaim, out var parsed))
                currentUserId = parsed;

            var result = await followService.GetStatusAsync(providerId, currentUserId);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }
    }
}
