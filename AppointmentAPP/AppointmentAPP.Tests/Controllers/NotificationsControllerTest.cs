using AppointmentAPP.Controller;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AppointmentAPP.Tests.Controllers;

public class NotificationsControllerTest
{
    private readonly Mock<INotificationService> _service = new();
    private readonly NotificationsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public NotificationsControllerTest()
    {
        _controller = new NotificationsController(_service.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetMy_ReturnsOk_AndCallsServiceWithUserId()
    {
        var result = await _controller.GetMy();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetMyNotificationsAsync(_userId), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.MarkAsRead(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.MarkAsReadAsync(_userId, id), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsOk_AndCallsServiceWithUserId()
    {
        var result = await _controller.MarkAllAsRead();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.MarkAllAsReadAsync(_userId), Times.Once);
    }
}