using AppointmentAPP.Dtos.UnavailableDayDtos;
using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UnavailableDaysController(
        IUnavailableDayService unavailableDayService,
        IValidator<CreateUnavailableDayDto> validator
    ) : BaseController
    {
        [Authorize(Roles = "Provider")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUnavailableDayDto dto)
        {
            var validation = validator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await unavailableDayService.CreateAsync(User.GetUserId(), dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [AllowAnonymous]
        [HttpGet("provider/{providerId:guid}")]
        public async Task<IActionResult> GetByProvider(Guid providerId)
        {
            var result = await unavailableDayService.GetByProviderAsync(providerId);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await unavailableDayService.DeleteAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Unavailable day removed."));
        }
    }
}
