using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.ServiceDtos;
using AppointmentAPP.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace AppointmentAPP.Tests.Controllers;

public class ServicesControllerTest
{
    private readonly Mock<IServiceManagementService> _service = new();
    private readonly Mock<IValidator<CreateServiceDto>> _createValidator = new();
    private readonly Mock<IValidator<UpdateServiceDto>> _updateValidator = new();

    private readonly ServicesController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public ServicesControllerTest()
    {
        _controller = new ServicesController(
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
    //  CREATE  (validasiya uğursuz / uğurlu)
    // =====================================================================

    [Fact]
    public async Task Create_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var dto = new CreateServiceDto();
        _createValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Ad boş ola bilməz"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreateServiceDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_ValidModel_ReturnsOk_AndCallsServiceWithUserId()
    {
        var dto = new CreateServiceDto();
        _createValidator.Setup(v => v.Validate(dto)).Returns(Valid());

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
    //  GET MY SERVICES
    // =====================================================================

    [Fact]
    public async Task GetMyServices_ReturnsOk_AndCallsServiceWithUserId()
    {
        var result = await _controller.GetMyServices();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetMyServicesAsync(_userId), Times.Once);
    }

    // =====================================================================
    //  UPDATE  (validasiya uğursuz / uğurlu)
    // =====================================================================

    [Fact]
    public async Task Update_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateServiceDto();
        _updateValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Ad boş ola bilməz"));

        var result = await _controller.Update(id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<UpdateServiceDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_ValidModel_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateServiceDto();
        _updateValidator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.Update(id, dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.UpdateAsync(_userId, id, dto), Times.Once);
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

    // =====================================================================
    //  GET ALL FOR ADMIN  (filtrsiz / filtrli)
    // =====================================================================

    [Fact]
    public async Task GetAllForAdmin_NoFilters_ReturnsOk_AndPassesNulls()
    {
        var result = await _controller.GetAllForAdmin(null, null);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAllForAdminAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task GetAllForAdmin_WithFilters_ReturnsOk_AndPassesArgs()
    {
        var providerId = Guid.NewGuid();

        var result = await _controller.GetAllForAdmin(providerId, true);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAllForAdminAsync(providerId, true), Times.Once);
    }

    // =====================================================================
    //  DEACTIVATE / REACTIVATE BY ADMIN  (yalnız id)
    // =====================================================================

    [Fact]
    public async Task DeactivateByAdmin_ReturnsOk_AndCallsServiceWithId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.DeactivateByAdmin(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.DeactivateByAdminAsync(id), Times.Once);
    }

    [Fact]
    public async Task ReactivateByAdmin_ReturnsOk_AndCallsServiceWithId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.ReactivateByAdmin(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.ReactivateByAdminAsync(id), Times.Once);
    }

    // =====================================================================
    //  GET CATALOG  (anonim, parametrsiz)
    // =====================================================================

    [Fact]
    public async Task GetCatalog_ReturnsOk_AndCallsService()
    {
        var result = await _controller.GetCatalog();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetCatalogAsync(), Times.Once);
    }

    // =====================================================================
    //  SEARCH  (anonim, ad parametri)
    // =====================================================================

    [Fact]
    public async Task Search_ReturnsOk_AndPassesName()
    {
        var result = await _controller.Search("dental");

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.SearchByNameAsync("dental"), Times.Once);
    }
}