using AppointmentAPP.Controller;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AppointmentAPP.Tests.Controllers;

public class FollowControllerTest
{
    private readonly Mock<IFollowService> _service = new();
    private readonly FollowController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public FollowControllerTest()
    {
        _controller = new FollowController(_service.Object);

        // User null olmasın deyə default (boş/anonim) kontekst qururuq.
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // nameIdentifier null verilsə -> heç bir claim yoxdur (anonim)
    private void SetUser(string? nameIdentifier)
    {
        var claims = new List<Claim>();
        if (nameIdentifier is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));

        _controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    // =====================================================================
    //  FOLLOW
    // =====================================================================

    [Fact]
    public async Task Follow_ReturnsOk_AndCallsServiceWithUserIdAndProviderId()
    {
        var providerId = Guid.NewGuid();
        SetUser(_userId.ToString());

        var result = await _controller.Follow(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.FollowAsync(_userId, providerId), Times.Once);
    }

    // =====================================================================
    //  UNFOLLOW
    // =====================================================================

    [Fact]
    public async Task Unfollow_ReturnsOk_AndCallsServiceWithUserIdAndProviderId()
    {
        var providerId = Guid.NewGuid();
        SetUser(_userId.ToString());

        var result = await _controller.Unfollow(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.UnfollowAsync(_userId, providerId), Times.Once);
    }

    // =====================================================================
    //  GET STATUS  (3 branch: düzgün Guid / anonim / Guid olmayan claim)
    // =====================================================================

    [Fact]
    public async Task GetStatus_AuthenticatedUser_PassesParsedUserId()
    {
        var providerId = Guid.NewGuid();
        SetUser(_userId.ToString());

        var result = await _controller.GetStatus(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetStatusAsync(providerId, _userId), Times.Once);
    }

    [Fact]
    public async Task GetStatus_AnonymousUser_PassesNull()
    {
        var providerId = Guid.NewGuid();
        // claim təyin edilmir -> idClaim null -> currentUserId null qalır

        var result = await _controller.GetStatus(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetStatusAsync(providerId, null), Times.Once);
    }

    [Fact]
    public async Task GetStatus_InvalidGuidClaim_PassesNull()
    {
        var providerId = Guid.NewGuid();
        SetUser("not-a-guid"); // Guid.TryParse uğursuz olur -> currentUserId null qalır

        var result = await _controller.GetStatus(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetStatusAsync(providerId, null), Times.Once);
    }
}