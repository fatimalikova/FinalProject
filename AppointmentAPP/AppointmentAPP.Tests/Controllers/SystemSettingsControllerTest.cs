using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.SystemSetting;
using AppointmentAPP.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AppointmentAPP.Tests.Controllers
{
    public class SystemSettingsControllerTest
    {
        private readonly Mock<ISystemSettingService> _service = new();
        private readonly Mock<IValidator<UpdateSystemSettingDto>> _validator = new();
        private readonly SystemSettingsController _controller;

        public SystemSettingsControllerTest()
        {
            _controller = new SystemSettingsController(_service.Object, _validator.Object);
        }

        private static ValidationResult Valid() => new();
        private static ValidationResult Invalid(string msg) =>
            new(new[] { new ValidationFailure("field", msg) });

        // =====================================================================
        //  GET  (parametrsiz delegasiya)
        // =====================================================================

        [Fact]
        public async Task Get_ReturnsOk_AndCallsService()
        {
            var result = await _controller.Get();

            Assert.IsType<OkObjectResult>(result);
            _service.Verify(s => s.GetAsync(), Times.Once);
        }

        // =====================================================================
        //  UPDATE  (validasiya uğursuz / uğurlu)
        // =====================================================================

        [Fact]
        public async Task Update_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
        {
            var dto = new UpdateSystemSettingDto();
            _validator.Setup(v => v.Validate(dto)).Returns(Invalid("Dəyər boş ola bilməz"));

            var result = await _controller.Update(dto);

            Assert.IsType<BadRequestObjectResult>(result);
            _service.Verify(s => s.UpdateAsync(It.IsAny<UpdateSystemSettingDto>()), Times.Never);
        }

        [Fact]
        public async Task Update_ValidModel_ReturnsOk_AndCallsServiceWithDto()
        {
            var dto = new UpdateSystemSettingDto();
            _validator.Setup(v => v.Validate(dto)).Returns(Valid());

            var result = await _controller.Update(dto);

            Assert.IsType<OkObjectResult>(result);
            _service.Verify(s => s.UpdateAsync(dto), Times.Once);
        }
    }
}
