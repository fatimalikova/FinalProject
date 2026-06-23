using AppointmentAPP.Dtos.ReviewDtos;
using AppointmentAPP.Extensions;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController(IReviewService reviewService, IValidator<CreateReviewDto> validator) : BaseController
    {
        [Authorize(Roles = "Client")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var validation = validator.Validate(dto);
            if (!validation.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var result = await reviewService.CreateAsync(User.GetUserId(), dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [AllowAnonymous]
        [HttpGet("provider/{providerId:guid}")]
        public async Task<IActionResult> GetByProvider(Guid providerId)
        {
            var result = await reviewService.GetByProviderAsync(providerId);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Client")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await reviewService.DeleteAsync(User.GetUserId(), id);
            return Ok(ResponseModelHelper.SuccessResult("Review deleted."));
        }
    }
}
