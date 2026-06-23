using AppointmentAPP.Dtos.ServiceDtos;
using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    public class ServicesController(
        IServiceManagementService serviceManagementService,
        IValidator<CreateServiceDto> createValidator,
        IValidator<UpdateServiceDto> updateValidator
    ) : BaseController
    {
        [Authorize(Roles = "Provider")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateServiceDto dto)
        {
            var validation = createValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await serviceManagementService.CreateAsync(User.GetUserId(), dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [AllowAnonymous]
        [HttpGet("provider/{providerId:guid}")]
        public async Task<IActionResult> GetByProvider(Guid providerId)
        {
            var result = await serviceManagementService.GetByProviderAsync(providerId);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyServices()
        {
            var result = await serviceManagementService.GetMyServicesAsync(User.GetUserId());
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceDto dto)
        {
            var validation = updateValidator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await serviceManagementService.UpdateAsync(User.GetUserId(), id, dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Provider")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await serviceManagementService.DeleteAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Service deleted."));
        }


        [Authorize(Roles = "Admin")]
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllForAdmin([FromQuery] Guid? providerId, [FromQuery] bool? isActive)
        {
            var result = await serviceManagementService.GetAllForAdminAsync(providerId, isActive);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/admin-deactivate")]
        public async Task<IActionResult> DeactivateByAdmin(Guid id)
        {
            await serviceManagementService.DeactivateByAdminAsync(id);
            return Ok(ResponseModelHelper.SuccessResult("Service deactivated by admin."));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}/admin-reactivate")]
        public async Task<IActionResult> ReactivateByAdmin(Guid id)
        {
            await serviceManagementService.ReactivateByAdminAsync(id);
            return Ok(ResponseModelHelper.SuccessResult("Service reactivated by admin."));
        }


        [AllowAnonymous]
        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog()
        {
            var result = await serviceManagementService.GetCatalogAsync();
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string name)
        {
            var result = await serviceManagementService.SearchByNameAsync(name);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }
    }
}