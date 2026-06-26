using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.Availability;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AppointmentAPP.Tests.Controllers;

public class AvailabilityControllerTest
{
    private readonly Mock<IAvailabilityService> _service = new();
    private readonly AvailabilityController _controller;

    public AvailabilityControllerTest()
    {
        _controller = new AvailabilityController(_service.Object);
    }

    [Fact]
    public async Task GetSlots_ReturnsOk_AndCallsServiceWithRequest()
    {
        var request = new AvailabilityRequestDto();

        var result = await _controller.GetSlots(request);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAvailableSlotsAsync(request), Times.Once);
    }
}