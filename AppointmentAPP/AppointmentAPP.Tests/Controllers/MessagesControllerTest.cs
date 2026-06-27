using AppointmentAPP.Controller;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AppointmentAPP.Tests.Controllers;

public class MessagesControllerTest
{
    private readonly Mock<IMessageService> _service = new();
    private readonly MessagesController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public MessagesControllerTest()
    {
        _controller = new MessagesController(_service.Object);

        // User.GetUserId() NameIdentifier claim-indən oxuyur deyə qəbul edirik.
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetMyConversations_ReturnsOk_AndCallsServiceWithUserId()
    {
        var result = await _controller.GetMyConversations();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetMyConversationsAsync(_userId), Times.Once);
    }

    [Fact]
    public async Task GetMessages_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.GetMessages(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetConversationMessagesAsync(_userId, id), Times.Once);
    }

    [Fact]
    public async Task MarkAsRead_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.MarkAsRead(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.MarkAsReadAsync(_userId, id), Times.Once);
    }
}