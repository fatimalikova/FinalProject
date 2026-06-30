using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.WorkingHourDtos;
using AppointmentAPP.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AppointmentAPP.Tests.Controllers
{
    public class WorkingHoursControllerTest
    {
        private readonly Mock<IWorkingHourService> _service = new();
        private readonly Mock<IValidator<CreateWorkingHourDto>> _validator = new();

        private readonly WorkingHoursController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public WorkingHoursControllerTest()
        {
            _controller = new WorkingHoursController(_service.Object, _validator.Object);

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
        //  SET  (siyahı qəbul edir, hər elementi loop ilə yoxlayır)
        // =====================================================================

        [Fact]
        public async Task Set_AllValid_ReturnsOk_AndCallsServiceWithUserIdAndList()
        {
            var dtos = new List<CreateWorkingHourDto> { new(), new() };
            _validator.Setup(v => v.Validate(It.IsAny<CreateWorkingHourDto>())).Returns(Valid());

            var result = await _controller.Set(dtos);

            Assert.IsType<OkObjectResult>(result);
            _service.Verify(s => s.SetWorkingHoursAsync(_userId, dtos), Times.Once);
        }

        [Fact]
        public async Task Set_OneInvalidItem_ReturnsBadRequest_AndDoesNotCallService()
        {
            var validDto = new CreateWorkingHourDto();
            var invalidDto = new CreateWorkingHourDto();
            var dtos = new List<CreateWorkingHourDto> { validDto, invalidDto };

            _validator.Setup(v => v.Validate(validDto)).Returns(Valid());
            _validator.Setup(v => v.Validate(invalidDto)).Returns(Invalid("Saat aralığı yanlışdır"));

            var result = await _controller.Set(dtos);

            Assert.IsType<BadRequestObjectResult>(result);
            _service.Verify(s => s.SetWorkingHoursAsync(
                It.IsAny<Guid>(), It.IsAny<List<CreateWorkingHourDto>>()), Times.Never);
        }

        [Fact]
        public async Task Set_EmptyList_SkipsValidation_ReturnsOk_AndCallsService()
        {
            var dtos = new List<CreateWorkingHourDto>();

            var result = await _controller.Set(dtos);

            Assert.IsType<OkObjectResult>(result);
            // boş siyahı -> loop heç işləmir -> validator çağırılmır, birbaşa servisə gedir
            _validator.Verify(v => v.Validate(It.IsAny<CreateWorkingHourDto>()), Times.Never);
            _service.Verify(s => s.SetWorkingHoursAsync(_userId, dtos), Times.Once);
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
}
