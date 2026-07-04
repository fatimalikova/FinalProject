using AppointmentAPP.Dtos.WorkingHourDtos;
using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    public class WorkingHoursController(
        IWorkingHourService workingHourService,
        IValidator<CreateWorkingHourDto> validator
    ) : BaseController
    {
        [Authorize(Roles = "Provider")]
        [HttpPut]
        public async Task<IActionResult> Set([FromBody] SetWorkingHoursDto request)
        {
            foreach (var dto in request.Dtos)
            {
                var validation = validator.Validate(dto);
                if (!validation.IsValid)
                    return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                        validation.Errors.Select(e => e.ErrorMessage).ToArray()));
            }

            var result = await workingHourService.SetWorkingHoursAsync(User.GetUserId(), request.Dtos);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [AllowAnonymous]
        [HttpGet("provider/{providerId:guid}")]
        public async Task<IActionResult> GetByProvider(Guid providerId)
        {
            var result = await workingHourService.GetByProviderAsync(providerId);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await workingHourService.DeleteAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Working hour removed."));
        }
    }
}