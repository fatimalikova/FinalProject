using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.Availability;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AvailabilityController(IAvailabilityService availabilityService) : BaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetSlots([FromQuery] AvailabilityRequestDto request)
        {
            var result = await availabilityService.GetAvailableSlotsAsync(request);
            return Ok(ResponseModelHelper.SuccessResult(result));
        }
    }
}