using AppointmentAPP.Dtos.SystemSetting;
using AppointmentAPP.Helpers;
using AppointmentAPP.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SystemSettingsController(
        ISystemSettingService systemSettingService,
        IValidator<UpdateSystemSettingDto> validator
    ) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var result = await systemSettingService.GetAsync();
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateSystemSettingDto dto)
        {
            var validation = validator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await systemSettingService.UpdateAsync(dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }
    }
}