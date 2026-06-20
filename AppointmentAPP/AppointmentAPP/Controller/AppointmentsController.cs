using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.AppointmentDtos;
using AppointmentAPP.Helpers;
using AppointmentAPP.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using AppointmentAPP.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{

    [Route("api/[controller]")]
    public class AppointmentsController(
        IAppointmentService appointmentService,
        IValidator<BookAppointmentDto> bookValidator,
        IValidator<RescheduleAppointmentDto> rescheduleValidator,
        IValidator<CancelAppointmentDto> cancelValidator
    ) : BaseController
    {
        [Authorize(Roles = "Client")]
        [HttpPost("book")]
        public async Task<IActionResult> Book([FromBody] BookAppointmentDto dto)
        {
            var validation = bookValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await appointmentService.BookAsync(User.GetUserId(), dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Client,Provider")]
        [HttpPut("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentDto dto)
        {
            var validation = cancelValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await appointmentService.CancelAsync(User.GetUserId(), id, dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Client,Provider")]
        [HttpPut("{id:guid}/reschedule")]
        public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleAppointmentDto dto)
        {
            var validation = rescheduleValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await appointmentService.RescheduleAsync(User.GetUserId(), id, dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpPut("{id:guid}/complete")]
        public async Task<IActionResult> Complete(Guid id)
        {
            var result = await appointmentService.CompleteAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        // F7 — Client history: ?filter=upcoming|past (boş buraxsan hamısı gəlir)
        [Authorize(Roles = "Client")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyAppointments([FromQuery] string? filter)
        {
            var result = await appointmentService.GetMyAppointmentsAsync(User.GetUserId(), filter);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        // F5 — Provider calendar: ?from=2026-06-19&to=2026-06-19 (gün) və ya &to=2026-06-25 (həftə)
        [Authorize(Roles = "Provider")]
        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendar([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var result = await appointmentService.GetProviderCalendarAsync(User.GetUserId(), from, to);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        //kimsə özünə aid olmayan appointment-in detalına baxa bilməsin deyə 
        [Authorize(Roles = "Client,Provider")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await appointmentService.GetByIdAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }


        [Authorize(Roles = "Client")]
        [HttpGet("me/profile")]
        public async Task<IActionResult> GetMyClientProfile()
        {
            var result = await appointmentService.GetMyClientProfileAsync(User.GetUserId());
            return Ok(ResponseModelHelper.SuccessResult(result));
        }
    }
}