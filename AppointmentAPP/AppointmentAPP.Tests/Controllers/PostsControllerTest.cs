using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.PostDtos;
using AppointmentAPP.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AppointmentAPP.Tests.Controllers;

public class PostsControllerTest
{
    private readonly Mock<IPostService> _service = new();
    private readonly Mock<IValidator<CreatePostDto>> _createPostValidator = new();
    private readonly Mock<IValidator<CreateCommentDto>> _createCommentValidator = new();

    private readonly PostsController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public PostsControllerTest()
    {
        _controller = new PostsController(
            _service.Object,
            _createPostValidator.Object,
            _createCommentValidator.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        SetUser(_userId.ToString()); // default: login olmuş istifadəçi
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

    private static ValidationResult Valid() => new();
    private static ValidationResult Invalid(string msg) =>
        new(new[] { new ValidationFailure("field", msg) });

    // =====================================================================
    //  CREATE  (validasiya uğursuz / uğurlu)
    // =====================================================================

    [Fact]
    public async Task Create_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var dto = new CreatePostDto();
        _createPostValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Mətn boş ola bilməz"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<CreatePostDto>()), Times.Never);
    }

    [Fact]
    public async Task Create_ValidModel_ReturnsOk_AndCallsServiceWithUserId()
    {
        var dto = new CreatePostDto();
        _createPostValidator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.Create(dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.CreateAsync(_userId, dto), Times.Once);
    }

    // =====================================================================
    //  DELETE POST
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
    //  GET BY PROVIDER  (3 branch: Guid / anonim / Guid olmayan claim)
    // =====================================================================

    [Fact]
    public async Task GetByProvider_AuthenticatedUser_PassesParsedUserId()
    {
        var providerId = Guid.NewGuid();
        SetUser(_userId.ToString());

        var result = await _controller.GetByProvider(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetByProviderAsync(providerId, _userId), Times.Once);
    }

    [Fact]
    public async Task GetByProvider_AnonymousUser_PassesNull()
    {
        var providerId = Guid.NewGuid();
        SetUser(null); // heç bir claim yoxdur

        var result = await _controller.GetByProvider(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetByProviderAsync(providerId, null), Times.Once);
    }

    [Fact]
    public async Task GetByProvider_InvalidGuidClaim_PassesNull()
    {
        var providerId = Guid.NewGuid();
        SetUser("not-a-guid"); // Guid.TryParse uğursuz

        var result = await _controller.GetByProvider(providerId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetByProviderAsync(providerId, null), Times.Once);
    }

    // =====================================================================
    //  LIKE / UNLIKE
    // =====================================================================

    [Fact]
    public async Task Like_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.Like(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.LikeAsync(_userId, id), Times.Once);
    }

    [Fact]
    public async Task Unlike_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.Unlike(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.UnlikeAsync(_userId, id), Times.Once);
    }

    // =====================================================================
    //  ADD COMMENT  (validasiya uğursuz / uğurlu)
    // =====================================================================

    [Fact]
    public async Task AddComment_InvalidModel_ReturnsBadRequest_AndDoesNotCallService()
    {
        var id = Guid.NewGuid();
        var dto = new CreateCommentDto();
        _createCommentValidator.Setup(v => v.Validate(dto)).Returns(Invalid("Şərh boş ola bilməz"));

        var result = await _controller.AddComment(id, dto);

        Assert.IsType<BadRequestObjectResult>(result);
        _service.Verify(s => s.AddCommentAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CreateCommentDto>()), Times.Never);
    }

    [Fact]
    public async Task AddComment_ValidModel_ReturnsOk_AndCallsServiceWithUserIdAndId()
    {
        var id = Guid.NewGuid();
        var dto = new CreateCommentDto();
        _createCommentValidator.Setup(v => v.Validate(dto)).Returns(Valid());

        var result = await _controller.AddComment(id, dto);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.AddCommentAsync(_userId, id, dto), Times.Once);
    }

    // =====================================================================
    //  GET COMMENTS  (anonim, yalnız id ötürülür)
    // =====================================================================

    [Fact]
    public async Task GetComments_ReturnsOk_AndCallsServiceWithId()
    {
        var id = Guid.NewGuid();

        var result = await _controller.GetComments(id);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.GetCommentsAsync(id), Times.Once);
    }

    // =====================================================================
    //  DELETE COMMENT
    // =====================================================================

    [Fact]
    public async Task DeleteComment_ReturnsOk_AndCallsServiceWithUserIdAndCommentId()
    {
        var commentId = Guid.NewGuid();

        var result = await _controller.DeleteComment(commentId);

        Assert.IsType<OkObjectResult>(result);
        _service.Verify(s => s.DeleteCommentAsync(_userId, commentId), Times.Once);
    }
}