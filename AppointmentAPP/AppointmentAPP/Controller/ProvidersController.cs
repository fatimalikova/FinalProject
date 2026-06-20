using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.ProviderDtos;
using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    public class ProvidersController(
        IProviderService providerService,
        IValidator<CreateProviderDto> createValidator,
        IValidator<UpdateProviderDto> updateValidator
    ) : BaseController
    {
        [Authorize(Roles = "Provider")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] CreateProviderDto dto)
        {
            var validation = createValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await providerService.RegisterAsync(User.GetUserId(), dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await providerService.GetAllAsync(category, page, pageSize);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        // 🔑 F8 — admin pending provider-ləri də görə bilsin
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllForAdmin([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await providerService.GetAllForAdminAsync(status, page, pageSize);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await providerService.GetByIdAsync(id);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await providerService.GetMyProfileAsync(User.GetUserId());
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProviderDto dto)
        {
            var validation = updateValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await providerService.UpdateAsync(User.GetUserId(), dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            await providerService.ApproveAsync(id);
            return Ok(ResponseModelHelper.SuccessResult("Provider approved."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/reject")]
        public async Task<IActionResult> Reject(Guid id)
        {
            await providerService.RejectAsync(id);
            return Ok(ResponseModelHelper.SuccessResult("Provider rejected."));
        }


        [Authorize(Roles = "Provider")]
        [HttpGet("me/dashboard")]
        public async Task<IActionResult> GetMyDashboard()
        {
            var result = await providerService.GetMyDashboardAsync(User.GetUserId());
            return Ok(ResponseModelHelper.SuccessResult(result));
        }
    }
}