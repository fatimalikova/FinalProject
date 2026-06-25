using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.AppointmentDtos;
using AppointmentAPP.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AppointmentAPP.Tests.Controllers;

public class AppointmentsControllerTest
{
    private readonly Mock<IAppointmentService> _service = new();
    private readonly Mock<IValidator<BookAppointmentDto>> _bookValidator = new();
    private readonly Mock<IValidator<RescheduleAppointmentDto>> _rescheduleValidator = new();
    private readonly Mock<IValidator<CancelAppointmentDto>> _cancelValidator = new();

    private readonly AppointmentsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public AppointmentsControllerTest()
    {
        _controller = new AppointmentsController(
            _service.Object,
            _bookValidator.Object,
            _rescheduleValidator.Object,
            _cancelValidator.Object);

        // User.GetUserId() NameIdentifier claim-indən oxuyur deyə qəbul edirik.
        // Hər test üçün etibarlı Guid-li istifadəçi konteksti qururuq.
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
    //  BOOK
    // =====================================================================

    [Fact]
    public async Task Book_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var dto = new BookAppointmentDto();
        _bookValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Tarix boş ola bilməz"));

        var result = await _controller.Book(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.BookAsync(It.IsAny<Guid>(), It.IsAny<BookAppointmentDto>()), Times.Never);
    }

    [Fact]
    public async Task Book_ValidModel_ReturnsOk_AndCallsServiceWithUserId()
    {
        var dto = new BookAppointmentDto();
        _bookValidator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.Book(dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.BookAsync(_userId, dto), Times.Once);
    }

    // =====================================================================
    //  CANCEL
    // =====================================================================

    [Fact]
    public async Task Cancel_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var id = Guid.NewGuid();
        var dto = new CancelAppointmentDto();
        _cancelValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Səbəb boş ola bilməz"));

        var result = await _controller.Cancel(id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.CancelAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancelAppointmentDto>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ValidModel_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();
        var dto = new CancelAppointmentDto();
        _cancelValidator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.Cancel(id, dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.CancelAsync(_userId, id, dto), Times.Once);
    }

    // =====================================================================
    //  RESCHEDULE
    // =====================================================================

    [Fact]
    public async Task Reschedule_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var id = Guid.NewGuid();
        var dto = new RescheduleAppointmentDto();
        _rescheduleValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Yeni tarix boş ola bilməz"));

        var result = await _controller.Reschedule(id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.RescheduleAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<RescheduleAppointmentDto>()), Times.Never);
    }

    [Fact]
    public async Task Reschedule_ValidModel_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();
        var dto = new RescheduleAppointmentDto();
        _rescheduleValidator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.Reschedule(id, dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.RescheduleAsync(_userId, id, dto), Times.Once);
    }

    // =====================================================================
    //  COMPLETE  (validasiyasız tək yol)
    // =====================================================================

    [Fact]
    public async Task Complete_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.Complete(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.CompleteAsync(_userId, id), Times.Once);
    }

    // =====================================================================
    //  GET MY APPOINTMENTS  (filter var / yoxdur)
    // =====================================================================

    [Theory]
    [InlineData("upcoming")]
    [InlineData("past")]
    [InlineData(null)]
    public async Task GetMyAppointments_PassesFilterToService_ReturnsOk(string? filter)
    {
        var result = await _controller.GetMyAppointments(filter);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetMyAppointmentsAsync(_userId, filter), Times.Once);
    }

    // =====================================================================
    //  GET CALENDAR
    // =====================================================================

    [Fact]
    public async Task GetCalendar_ReturnsOk_AndPassesDateRange()
    {
        var from = new DateTime(2026, 6, 19);
        var to = new DateTime(2026, 6, 25);

        var result = await _controller.GetCalendar(from, to);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetProviderCalendarAsync(_userId, from, to), Times.Once);
    }

    // =====================================================================
    //  GET BY ID
    // =====================================================================

    [Fact]
    public async Task GetById_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.GetById(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetByIdAsync(_userId, id), Times.Once);
    }

    // =====================================================================
    //  GET MY CLIENT PROFILE
    // =====================================================================

    [Fact]
    public async Task GetMyClientProfile_ReturnsOk_AndCallsService()
    {
        var result = await _controller.GetMyClientProfile();

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetMyClientProfileAsync(_userId), Times.Once);
    }

    // =====================================================================
    //  GET ALL FOR ADMIN  (filterli / filtersiz)
    // =====================================================================

    [Fact]
    public async Task GetAllForAdmin_WithFilters_ReturnsOk_AndPassesArgs()
    {
        var from = new DateTime(2026, 6, 1);
        var to = new DateTime(2026, 6, 30);

        var result = await _controller.GetAllForAdmin("Completed", from, to);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAllForAdminAsync("Completed", from, to), Times.Once);
    }

    [Fact]
    public async Task GetAllForAdmin_NoFilters_ReturnsOk_AndPassesNulls()
    {
        var result = await _controller.GetAllForAdmin(null, null, null);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetAllForAdminAsync(null, null, null), Times.Once);
    }
}