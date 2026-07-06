using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.SliderDtos;
using AppointmentAPP.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AppointmentAPP.Tests.Controllers
{
    public class SlidersControllerTest
    {
        private readonly Mock<ISliderService> _service;
        private readonly SlidersController _controller;

        public SlidersControllerTest()
        {
            _service = new Mock<ISliderService>();
            _controller = new SlidersController(_service.Object);
        }

        //==================================================
        // GET ACTIVE
        //==================================================

        [Fact]
        public async Task GetActive_ReturnsOk_AndCallsService()
        {
            // Arrange
            var sliders = new List<ResponseSliderDto>();

            _service.Setup(x => x.GetActiveAsync())
                    .ReturnsAsync(sliders);

            // Act
            var result = await _controller.GetActive();

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x => x.GetActiveAsync(), Times.Once);
        }

        [Fact]
        public async Task GetActive_WhenServiceReturnsEmptyList_ReturnsOk()
        {
            // Arrange
            _service.Setup(x => x.GetActiveAsync())
                    .ReturnsAsync(new List<ResponseSliderDto>());

            // Act
            var result = await _controller.GetActive();

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x => x.GetActiveAsync(), Times.Once);
        }

        //==================================================
        // GET ALL
        //==================================================

        [Fact]
        public async Task GetAll_ReturnsOk_AndCallsService()
        {
            // Arrange
            var sliders = new List<ResponseSliderDto>();

            _service.Setup(x => x.GetAllAsync())
                    .ReturnsAsync(sliders);

            // Act
            var result = await _controller.GetAll();

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAll_WhenServiceReturnsEmptyList_ReturnsOk()
        {
            // Arrange
            _service.Setup(x => x.GetAllAsync())
                    .ReturnsAsync(new List<ResponseSliderDto>());

            // Act
            var result = await _controller.GetAll();

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x => x.GetAllAsync(), Times.Once);
        }

        //==================================================
        // CREATE
        //==================================================

        [Fact]
        public async Task Create_ReturnsOk_AndCallsService()
        {
            // Arrange
            var dto = new CreateSliderDto();

            var created = new ResponseSliderDto();

            _service.Setup(x => x.CreateAsync(dto))
                    .ReturnsAsync(created);

            // Act
            var result = await _controller.Create(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x => x.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_WhenServiceThrowsException_ThrowsException()
        {
            // Arrange
            var dto = new CreateSliderDto();

            _service.Setup(x => x.CreateAsync(dto))
                    .ThrowsAsync(new Exception());

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.Create(dto));
        }

        //==================================================
        // UPDATE
        //==================================================

        [Fact]
        public async Task Update_ReturnsOk_AndCallsService()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new UpdateSliderDto();

            var updated = new ResponseSliderDto();

            _service.Setup(x => x.UpdateAsync(id, dto))
                    .ReturnsAsync(updated);

            // Act
            var result = await _controller.Update(id, dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x => x.UpdateAsync(id, dto), Times.Once);
        }

        [Fact]
        public async Task Update_WhenServiceThrowsException_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();

            var dto = new UpdateSliderDto();

            _service.Setup(x => x.UpdateAsync(id, dto))
                    .ThrowsAsync(new Exception());

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.Update(id, dto));
        }

        //==================================================
        // DELETE
        //==================================================

        [Fact]
        public async Task Delete_ReturnsOk_AndCallsService()
        {
            // Arrange
            var id = Guid.NewGuid();

            _service.Setup(x => x.DeleteAsync(id))
                    .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<OkObjectResult>(result);

            _service.Verify(x => x.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task Delete_WhenServiceThrowsException_ThrowsException()
        {
            // Arrange
            var id = Guid.NewGuid();

            _service.Setup(x => x.DeleteAsync(id))
                    .ThrowsAsync(new Exception());

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.Delete(id));
        }
    }
}
