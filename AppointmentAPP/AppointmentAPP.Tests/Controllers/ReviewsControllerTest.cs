using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.ReviewDtos;
using AppointmentAPP.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AppointmentAPP.Tests.Controllers;

public class ReviewsControllerTest
{
    private readonly Mock<IReviewService> _service = new();
    private readonly Mock<IValidator<CreateReviewDto>> _validator = new();

    private readonly ReviewsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ReviewsControllerTest()
    {
        _controller = new ReviewsController(_service.Object, _validator.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static ValidationResult Valid() => new();
    private static ValidationResult Invalid(string msg) =>
        new(new[] { new ValidationFailure("field", msg) });

    // =====================================================================
    //  CREATE  (validasiya uğursuz / uğurlu)
    // =====================================================================

    [Fact]
    public async Task Create_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var dto = new CreateReviewDto();
        _validator.Setup(v => v.Validate(dto)).Returns(Invalid("Reytinq boş ola bilməz"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateReviewDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_ValidModel_ReturnsOk_AndCallsServiceWithUserId()
    {
        var dto = new CreateReviewDto();
        _validator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.CreateAsync(_userId, dto), Times.Once);
    }

    // =====================================================================
    //  GET BY PROVIDER  (anonim, yalnız id)
    // =====================================================================

    [Fact]
    public async Task GetByProvider_ReturnsOk_AndCallsServiceWithProviderId()
    {
        var providerId = Guid.NewGuid();

        var result = await _controller.GetByProvider(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetByProviderAsync(providerId), Times.Once);
    }

    // =====================================================================
    //  DELETE
    // =====================================================================

    [Fact]
    public async Task Delete_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.Delete(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.DeleteAsync(_userId, id), Times.Once);
    }
}