using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.ProviderDtos;
using AppointmentAPP.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AppointmentAPP.Tests.Controllers;

public class ProvidersControllerTest
{
    private readonly Mock<IProviderService> _service = new();
    private readonly Mock<IValidator<CreateProviderDto>> _createValidator = new();
    private readonly Mock<IValidator<UpdateProviderDto>> _updateValidator = new();

    private readonly ProvidersController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ProvidersControllerTest()
    {
        _controller = new ProvidersController(
            _service.Object,
            _createValidator.Object,
            _updateValidator.Object);

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
    //  REGISTER  (validasiya uğursuz / uğurlu)
    // =====================================================================

    [Fact]
    public async Task Register_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var dto = new CreateProviderDto();
        _createValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Ad boş ola bilməz"));

        var result = await _controller.Register(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.RegisterAsync(It.IsAny<Guid>(), It.IsAny<CreateProviderDto>()), Times.Never);
    }

    [Fact]
    public async Task Register_ValidModel_ReturnsOk_AndCallsServiceWithUserId()
    {
        var dto = new CreateProviderDto();
        _createValidator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.Register(dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.RegisterAsync(_userId, dto), Times.Once);
    }

    // =====================================================================
    //  GET ALL  (default səhifələmə / açıq verilən dəyərlər / kateqoriya)
    // =====================================================================

    [Fact]
    public async Task GetAll_DefaultPaging_ReturnsOk_AndPassesDefaults()
    {
        var result = await _controller.GetAll(null);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAllAsync(null, 1, 10), Times.Once); // default page=1, pageSize=10
    }

    [Fact]
    public async Task GetAll_WithCategoryAndPaging_ReturnsOk_AndPassesArgs()
    {
        var result = await _controller.GetAll("dentist", 2, 25);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAllAsync("dentist", 2, 25), Times.Once);
    }

    // =====================================================================
    //  GET ALL FOR ADMIN  (default səhifələmə / status ilə)
    // =====================================================================

    [Fact]
    public async Task GetAllForAdmin_DefaultPaging_ReturnsOk_AndPassesDefaults()
    {
        var result = await _controller.GetAllForAdmin(null);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAllForAdminAsync(null, 1, 20), Times.Once); // default page=1, pageSize=20
    }

    [Fact]
    public async Task GetAllForAdmin_WithStatusAndPaging_ReturnsOk_AndPassesArgs()
    {
        var result = await _controller.GetAllForAdmin("Pending", 3, 50);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAllForAdminAsync("Pending", 3, 50), Times.Once);
    }

    // =====================================================================
    //  GET BY ID  (anonim, yalnız id)
    // =====================================================================

    [Fact]
    public async Task GetById_ReturnsOk_AndCallsServiceWithId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.GetById(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetByIdAsync(id), Times.Once);
    }

    // =====================================================================
    //  GET MY PROFILE
    // =====================================================================

    [Fact]
    public async Task GetMyProfile_ReturnsOk_AndCallsServiceWithUserId()
    {
        var result = await _controller.GetMyProfile();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetMyProfileAsync(_userId), Times.Once);
    }

    // =====================================================================
    //  UPDATE MY PROFILE  (validasiya uğursuz / uğurlu)
    // =====================================================================

    [Fact]
    public async Task UpdateMyProfile_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var dto = new UpdateProviderDto();
        _updateValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Ad boş ola bilməz"));

        var result = await _controller.UpdateMyProfile(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateProviderDto>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMyProfile_ValidModel_ReturnsOk_AndCallsServiceWithUserId()
    {
        var dto = new UpdateProviderDto();
        _updateValidator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.UpdateMyProfile(dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.UpdateAsync(_userId, dto), Times.Once);
    }

    // =====================================================================
    //  APPROVE / REJECT  (admin, yalnız id)
    // =====================================================================

    [Fact]
    public async Task Approve_ReturnsOk_AndCallsServiceWithId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.Approve(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.ApproveAsync(id), Times.Once);
    }

    [Fact]
    public async Task Reject_ReturnsOk_AndCallsServiceWithId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.Reject(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.RejectAsync(id), Times.Once);
    }

    // =====================================================================
    //  GET MY DASHBOARD
    // =====================================================================

    [Fact]
    public async Task GetMyDashboard_ReturnsOk_AndCallsServiceWithUserId()
    {
        var result = await _controller.GetMyDashboard();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetMyDashboardAsync(_userId), Times.Once);
    }
}