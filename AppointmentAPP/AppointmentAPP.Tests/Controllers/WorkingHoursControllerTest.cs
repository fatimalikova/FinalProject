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

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, _userId.ToString())
            };

            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "TestAuth"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        private static ValidationResult Valid() => new();

        private static ValidationResult Invalid(string message) =>
            new(new[]
            {
                new ValidationFailure("Field", message)
            });

        // =====================================================================
        // SET
        // =====================================================================

        [Fact]
        public async Task Set_AllValid_ReturnsOk_AndCallsService()
        {
            // Arrange
            var request = new SetWorkingHoursDto
            {
                Dtos = new List<CreateWorkingHourDto>
                {
                    new(),
                    new()
                }
            };

            _validator.Setup(x => x.Validate(It.IsAny<CreateWorkingHourDto>()))
                      .Returns(Valid());

            // Act
            var result = await _controller.Set(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x =>
                x.SetWorkingHoursAsync(_userId, request.Dtos),
                Times.Once);
        }

        [Fact]
        public async Task Set_OneInvalid_ReturnsBadRequest_AndDoesNotCallService()
        {
            // Arrange
            var validDto = new CreateWorkingHourDto();
            var invalidDto = new CreateWorkingHourDto();

            var request = new SetWorkingHoursDto
            {
                Dtos = new List<CreateWorkingHourDto>
                {
                    validDto,
                    invalidDto
                }
            };

            _validator.Setup(x => x.Validate(validDto))
                      .Returns(Valid());

            _validator.Setup(x => x.Validate(invalidDto))
                      .Returns(Invalid("Working hour is invalid"));

            // Act
            var result = await _controller.Set(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);

            _service.Verify(x =>
                x.SetWorkingHoursAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<List<CreateWorkingHourDto>>()),
                Times.Never);
        }

        [Fact]
        public async Task Set_EmptyList_ReturnsOk_AndCallsService()
        {
            // Arrange
            var request = new SetWorkingHoursDto
            {
                Dtos = new List<CreateWorkingHourDto>()
            };

            // Act
            var result = await _controller.Set(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _validator.Verify(x =>
                x.Validate(It.IsAny<CreateWorkingHourDto>()),
                Times.Never);

            _service.Verify(x =>
                x.SetWorkingHoursAsync(_userId, request.Dtos),
                Times.Once);
        }

        // =====================================================================
        // GET BY PROVIDER
        // =====================================================================

        [Fact]
        public async Task GetByProvider_ReturnsOk_AndCallsService()
        {
            // Arrange
            var providerId = Guid.NewGuid();

            // Act
            var result = await _controller.GetByProvider(providerId);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x =>
                x.GetByProviderAsync(providerId),
                Times.Once);
        }

        // =====================================================================
        // DELETE
        // =====================================================================

        [Fact]
        public async Task Delete_ReturnsOk_AndCallsService()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x =>
                x.DeleteAsync(_userId, id),
                Times.Once);
        }
    }
}