using AppointmentAPP.Dtos.SliderDtos;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class SlidersController(ISliderService sliderService) : BaseController
    {
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetActive()
        {
            var result = await sliderService.GetActiveAsync();
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await sliderService.GetAllAsync();
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSliderDto dto)
        {
            var result = await sliderService.CreateAsync(dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSliderDto dto)
        {
            var result = await sliderService.UpdateAsync(id, dto);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await sliderService.DeleteAsync(id);
            return Ok(ResponseModelHelper.SuccessResult("Slider deleted."));
        }

    }
}
