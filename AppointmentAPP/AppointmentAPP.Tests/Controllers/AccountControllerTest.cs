using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.Login_RegisterDtos;
using AppointmentAPP.Dtos.ProfileDtos;
using AppointmentAPP.Dtos.UserDtos;
using AppointmentAPP.Exceptions;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace AppointmentAPP.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<IValidator<RegisterDto>> _registerValidatorMock = new();
        private readonly Mock<IValidator<LoginDto>> _loginValidatorMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IJwtService> _jwtServiceMock = new();
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly AccountController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public AccountControllerTests()
        {
            _userManagerMock = MockUserManager();
            _roleManagerMock = MockRoleManager();

            _controller = new AccountController(
                _registerValidatorMock.Object,
                _loginValidatorMock.Object,
                _mapperMock.Object,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _configMock.Object,
                _jwtServiceMock.Object,
                _emailServiceMock.Object
            );

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        /* ══════════════════════ HELPERS ══════════════════════ */

        private static Mock<UserManager<AppUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private static Mock<RoleManager<IdentityRole<Guid>>> MockRoleManager()
        {
            var store = new Mock<IRoleStore<IdentityRole<Guid>>>();
            return new Mock<RoleManager<IdentityRole<Guid>>>(
                store.Object, null, null, null, null);
        }

        private void SetUser(Guid? userId)
        {
            var claims = new List<Claim>();
            if (userId.HasValue)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        }

        private static JsonElement GetBody(IActionResult result)
        {
            var value = ((ObjectResult)result).Value;
            var json = JsonSerializer.Serialize(value);
            return JsonDocument.Parse(json).RootElement;
        }

        private static ValidationResult ValidResult() => new();
        private static ValidationResult InvalidResult(string message = "Invalid") =>
            new(new[] { new ValidationFailure("Field", message) });

        private AppUser NewUser(bool emailConfirmed = true) => new()
        {
            Id = _userId,
            UserName = "testuser",
            FullName = "Test User",
            Email = "test@example.com",
            EmailConfirmed = emailConfirmed,
            CreatedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        };

        /* ══════════════════════ REGISTER ══════════════════════ */

        [Fact]
        public async Task RegisterClient_InvalidDto_ReturnsBadRequest()
        {
            _registerValidatorMock.Setup(v => v.Validate(It.IsAny<RegisterDto>())).Returns(InvalidResult("UserName required"));

            var result = await _controller.RegisterClient(new RegisterDto());

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(GetBody(bad).GetProperty("Success").GetBoolean());
        }

        [Fact]
        public async Task RegisterClient_UsernameTaken_ReturnsBadRequest()
        {
            var dto = new RegisterDto { UserName = "existing", Email = "a@a.com", Password = "P@ss123!" };
            _registerValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(NewUser());

            var result = await _controller.RegisterClient(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User already exists", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task RegisterClient_EmailTaken_ReturnsBadRequest()
        {
            var dto = new RegisterDto { UserName = "newuser", Email = "taken@a.com", Password = "P@ss123!" };
            _registerValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser)null!);
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(NewUser());

            var result = await _controller.RegisterClient(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Email is already registered", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task RegisterClient_CreateAsyncFails_ReturnsBadRequest()
        {
            var dto = new RegisterDto { UserName = "newuser", Email = "new@a.com", Password = "weak" };
            var mappedUser = new AppUser { UserName = dto.UserName, Email = dto.Email };

            _registerValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser)null!);
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser)null!);
            _mapperMock.Setup(m => m.Map<AppUser>(dto)).Returns(mappedUser);
            _userManagerMock.Setup(m => m.CreateAsync(mappedUser, dto.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

            var result = await _controller.RegisterClient(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Password too weak", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task RegisterClient_AddToRoleFails_DeletesUserAndReturnsBadRequest()
        {
            var dto = new RegisterDto { UserName = "newuser", Email = "new@a.com", Password = "P@ss123!" };
            var mappedUser = new AppUser { UserName = dto.UserName, Email = dto.Email };

            _registerValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser)null!);
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser)null!);
            _mapperMock.Setup(m => m.Map<AppUser>(dto)).Returns(mappedUser);
            _userManagerMock.Setup(m => m.CreateAsync(mappedUser, dto.Password)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.AddToRoleAsync(mappedUser, "Client"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role does not exist" }));
            _userManagerMock.Setup(m => m.DeleteAsync(mappedUser)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.RegisterClient(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Role does not exist", GetBody(bad).GetProperty("Errors")[0].GetString());
            _userManagerMock.Verify(m => m.DeleteAsync(mappedUser), Times.Once);
        }

        [Fact]
        public async Task RegisterClient_Success_SendsConfirmationEmailAndReturnsOk()
        {
            var dto = new RegisterDto { UserName = "newuser", Email = "new@a.com", Password = "P@ss123!" };
            var mappedUser = new AppUser { UserName = dto.UserName, Email = dto.Email };

            _registerValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser)null!);
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser)null!);
            _mapperMock.Setup(m => m.Map<AppUser>(dto)).Returns(mappedUser);
            _userManagerMock.Setup(m => m.CreateAsync(mappedUser, dto.Password)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.AddToRoleAsync(mappedUser, "Client")).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(mappedUser)).ReturnsAsync("token123");

            var result = await _controller.RegisterClient(dto);

            Assert.IsType<OkObjectResult>(result);
            _emailServiceMock.Verify(e => e.SendEmailAsync(
                dto.Email, "Email Confirmation", It.Is<string>(s => s.Contains("token123"))
            ), Times.Once);
        }

        [Fact]
        public async Task RegisterProvider_Success_AssignsProviderRole()
        {
            var dto = new RegisterDto { UserName = "provuser", Email = "prov@a.com", Password = "P@ss123!" };
            var mappedUser = new AppUser { UserName = dto.UserName, Email = dto.Email };

            _registerValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser)null!);
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser)null!);
            _mapperMock.Setup(m => m.Map<AppUser>(dto)).Returns(mappedUser);
            _userManagerMock.Setup(m => m.CreateAsync(mappedUser, dto.Password)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.AddToRoleAsync(mappedUser, "Provider")).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(mappedUser)).ReturnsAsync("token456");

            var result = await _controller.RegisterProvider(dto);

            Assert.IsType<OkObjectResult>(result);
            _userManagerMock.Verify(m => m.AddToRoleAsync(mappedUser, "Provider"), Times.Once);
        }

        /* ══════════════════════ LOGIN ══════════════════════ */

        [Fact]
        public async Task LoginClient_InvalidDto_ReturnsBadRequest()
        {
            _loginValidatorMock.Setup(v => v.Validate(It.IsAny<LoginDto>())).Returns(InvalidResult());

            var result = await _controller.LoginClient(new LoginDto());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LoginClient_UserNotFound_ReturnsBadRequest()
        {
            var dto = new LoginDto { UserName = "nouser", Password = "x" };
            _loginValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser)null!);

            var result = await _controller.LoginClient(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid username or password", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task LoginClient_WrongPassword_ReturnsBadRequest()
        {
            var dto = new LoginDto { UserName = "user1", Password = "wrong" };
            var user = NewUser();
            _loginValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(false);

            var result = await _controller.LoginClient(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid username or password", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task LoginClient_EmailNotConfirmed_ReturnsBadRequest()
        {
            var dto = new LoginDto { UserName = "user1", Password = "correct" };
            var user = NewUser(emailConfirmed: false);
            _loginValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);

            var result = await _controller.LoginClient(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Email is not confirmed.", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task LoginClient_WrongRole_ReturnsBadRequest()
        {
            var dto = new LoginDto { UserName = "user1", Password = "correct" };
            var user = NewUser();
            _loginValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Provider" });

            var result = await _controller.LoginClient(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("This account cannot log in from the Client portal.", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task LoginClient_Success_Sends2FACodeAndReturnsOk()
        {
            var dto = new LoginDto { UserName = "user1", Password = "correct" };
            var user = NewUser();
            _loginValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.LoginClient(dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(user.TwoFactorCode);
            Assert.Equal(6, user.TwoFactorCode!.Length);
            Assert.NotNull(user.TwoFactorCodeExpiry);
            _emailServiceMock.Verify(e => e.SendEmailAsync(user.Email!, "2FA Kodu", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task LoginProvider_CorrectRole_Succeeds()
        {
            var dto = new LoginDto { UserName = "provider1", Password = "correct" };
            var user = NewUser();
            _loginValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Provider" });
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.LoginProvider(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task LoginAdmin_CorrectRole_Succeeds()
        {
            var dto = new LoginDto { UserName = "admin1", Password = "correct" };
            var user = NewUser();
            _loginValidatorMock.Setup(v => v.Validate(dto)).Returns(ValidResult());
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.LoginAdmin(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        /* ══════════════════════ VERIFY 2FA ══════════════════════ */

        [Fact]
        public async Task Verify2FA_UserNotFound_ReturnsNotFound()
        {
            var dto = new Verify2FADto { UserName = "nouser", Code = "123456" };
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser)null!);

            var result = await _controller.Verify2FA(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Verify2FA_CodeMismatch_ReturnsBadRequest()
        {
            var dto = new Verify2FADto { UserName = "user1", Code = "000000" };
            var user = NewUser();
            user.TwoFactorCode = "111111";
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(3);
            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);

            var result = await _controller.Verify2FA(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Kod yanlışdır və ya vaxtı keçib.", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task Verify2FA_CodeExpired_ReturnsBadRequest()
        {
            var dto = new Verify2FADto { UserName = "user1", Code = "111111" };
            var user = NewUser();
            user.TwoFactorCode = "111111";
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(-1);

            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);

            var result = await _controller.Verify2FA(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Verify2FA_NoCodeSet_ReturnsBadRequest()
        {
            var dto = new Verify2FADto { UserName = "user1", Code = "111111" };
            var user = NewUser();

            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);

            var result = await _controller.Verify2FA(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Verify2FA_Success_ReturnsTokensAndClearsCode()
        {
            var dto = new Verify2FADto { UserName = "user1", Code = "111111" };
            var user = NewUser();
            user.TwoFactorCode = "111111";
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(3);

            _userManagerMock.Setup(m => m.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });
            _jwtServiceMock.Setup(j => j.GenerateToken(user, It.IsAny<IList<string>>(), _configMock.Object)).Returns("access-token-value");
            _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token-value");
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Verify2FA(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = GetBody(ok).GetProperty("Data");
            Assert.Equal("access-token-value", data.GetProperty("accessToken").GetString());
            Assert.Equal("refresh-token-value", data.GetProperty("refreshToken").GetString());

            Assert.Null(user.TwoFactorCode);
            Assert.Null(user.TwoFactorCodeExpiry);
            Assert.Equal("refresh-token-value", user.RefreshToken);
            Assert.NotNull(user.RefreshTokenExpiry);
        }

        /* ══════════════════════ CONFIRM EMAIL ══════════════════════ */

        [Fact]
        public async Task ConfirmEmail_UserNotFound_ReturnsNotFound()
        {
            _userManagerMock.Setup(m => m.FindByEmailAsync("noone@a.com")).ReturnsAsync((AppUser)null!);

            var result = await _controller.ConfirmEmail("noone@a.com", "sometoken");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
        {
            var user = NewUser();
            _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "decoded-token"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

            var result = await _controller.ConfirmEmail(user.Email!, "decoded-token");

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid token.", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task ConfirmEmail_Success_ReturnsOk()
        {
            var user = NewUser();
            _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "decoded-token")).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ConfirmEmail(user.Email!, "decoded-token");

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmEmail_TokenIsUrlDecoded()
        {
            var user = NewUser();
            _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "a+b c")).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ConfirmEmail(user.Email!, Uri.EscapeDataString("a+b c"));

            Assert.IsType<OkObjectResult>(result);
            _userManagerMock.Verify(m => m.ConfirmEmailAsync(user, "a+b c"), Times.Once);
        }

        /* ══════════════════════ FORGOT PASSWORD ══════════════════════ */

        [Fact]
        public async Task ForgotPassword_UserNotFound_ReturnsNotFound()
        {
            var dto = new ForgotPasswordDto { Email = "noone@a.com" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser)null!);

            var result = await _controller.ForgotPassword(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ForgotPassword_Success_SendsEmailWithToken()
        {
            var user = NewUser();
            var dto = new ForgotPasswordDto { Email = user.Email! };
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token-xyz");

            var result = await _controller.ForgotPassword(dto);

            Assert.IsType<OkObjectResult>(result);
            _emailServiceMock.Verify(e => e.SendEmailAsync(
                dto.Email, "Reset Password", It.Is<string>(s => s.Contains("reset-token-xyz"))
            ), Times.Once);
        }

        /* ══════════════════════ RESET PASSWORD ══════════════════════ */

        [Fact]
        public async Task ResetPassword_UserNotFound_ReturnsNotFound()
        {
            var dto = new ResetPasswordDto { Email = "noone@a.com", Token = "t", NewPassword = "New@123" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser)null!);

            var result = await _controller.ResetPassword(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
        {
            var user = NewUser();
            var dto = new ResetPasswordDto { Email = user.Email!, Token = "bad-token", NewPassword = "New@123" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

            var result = await _controller.ResetPassword(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid token.", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task ResetPassword_Success_ReturnsOk()
        {
            var user = NewUser();
            var dto = new ResetPasswordDto { Email = user.Email!, Token = "good-token", NewPassword = "New@123" };
            _userManagerMock.Setup(m => m.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.ResetPasswordAsync(user, dto.Token, dto.NewPassword)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ResetPassword(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        /* ══════════════════════ REFRESH ══════════════════════ */

        [Fact]
        public async Task Refresh_InvalidAccessToken_ReturnsBadRequest()
        {
            var dto = new RefreshTokenDto { AccessToken = "bad", RefreshToken = "r" };
            _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, _configMock.Object))
                .Returns((ClaimsPrincipal)null!);

            var result = await _controller.Refresh(dto);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid access token", GetBody(bad).GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task Refresh_UserNotFound_ReturnsUnauthorized()
        {
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "r" };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) }));
            _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, _configMock.Object)).Returns(principal);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync((AppUser)null!);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_TokenMismatch_ReturnsUnauthorized()
        {
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "wrong-token" };
            var user = NewUser();
            user.RefreshToken = "correct-token";
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) }));
            _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, _configMock.Object)).Returns(principal);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_TokenExpired_ReturnsUnauthorized()
        {
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "correct-token" };
            var user = NewUser();
            user.RefreshToken = "correct-token";
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) }));
            _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, _configMock.Object)).Returns(principal);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_NullRefreshTokenExpiry_ReturnsUnauthorized()
        {
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "correct-token" };
            var user = NewUser();
            user.RefreshToken = "correct-token";
            user.RefreshTokenExpiry = null;

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) }));
            _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, _configMock.Object)).Returns(principal);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_Success_ReturnsNewTokens()
        {
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "correct-token" };
            var user = NewUser();
            user.RefreshToken = "correct-token";
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) }));
            _jwtServiceMock.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, _configMock.Object)).Returns(principal);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });
            _jwtServiceMock.Setup(j => j.GenerateToken(user, It.IsAny<IList<string>>(), _configMock.Object)).Returns("new-access-token");
            _jwtServiceMock.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh-token");
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Refresh(dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = GetBody(ok).GetProperty("Data");
            Assert.Equal("new-access-token", data.GetProperty("accessToken").GetString());
            Assert.Equal("new-refresh-token", data.GetProperty("refreshToken").GetString());
            Assert.Equal("new-refresh-token", user.RefreshToken);
        }

        /* ══════════════════════ PROFILE (GET) ══════════════════════ */

        [Fact]
        public async Task Profile_NoUserIdClaim_ReturnsNotFound()
        {
            SetUser(null);

            var result = await _controller.Profile();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Profile_UserNotFoundInDb_ReturnsNotFound()
        {
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync((AppUser)null!);

            var result = await _controller.Profile();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Profile_Success_ReturnsUserData()
        {
            var user = NewUser();
            user.ImageUrl = "https://example.com/a.jpg";
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });

            var result = await _controller.Profile();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = GetBody(ok).GetProperty("Data");
            Assert.Equal(user.UserName, data.GetProperty("userName").GetString());
            Assert.Equal(user.FullName, data.GetProperty("fullName").GetString());
            Assert.Equal(user.Email, data.GetProperty("email").GetString());
            Assert.Equal(user.ImageUrl, data.GetProperty("imageUrl").GetString());
        }

        [Fact]
        public async Task Profile_NoImageUrl_ReturnsNull()
        {
            var user = NewUser();
            user.ImageUrl = null;
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            var result = await _controller.Profile();

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = GetBody(ok).GetProperty("Data");
            Assert.Equal(JsonValueKind.Null, data.GetProperty("imageUrl").ValueKind);
        }

        /* ══════════════════════ LOGOUT ══════════════════════ */

        [Fact]
        public async Task Logout_NoUserIdClaim_StillReturnsOk()
        {
            SetUser(null);

            var result = await _controller.Logout();

            Assert.IsType<OkObjectResult>(result);
            _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Logout_UserNotFoundInDb_StillReturnsOk()
        {
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync((AppUser)null!);

            var result = await _controller.Logout();

            Assert.IsType<OkObjectResult>(result);
            _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Logout_UserFound_ClearsRefreshTokenAndReturnsOk()
        {
            var user = NewUser();
            user.RefreshToken = "some-token";
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(2);

            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Logout();

            Assert.IsType<OkObjectResult>(result);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiry);
            _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
        }

        /* ══════════════════════ UPDATE PROFILE (PUT) ══════════════════════ */

        [Fact]
        public async Task UpdateProfile_UserNotFound_ThrowsNotFoundException()
        {
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync((AppUser)null!);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _controller.UpdateProfile(new UpdateClientProfileDto { FullName = "New Name" }));
        }

        [Fact]
        public async Task UpdateProfile_UpdatesFullName()
        {
            var user = NewUser();
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.UpdateProfile(new UpdateClientProfileDto { FullName = "New Name" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("New Name", user.FullName);
            Assert.Equal("New Name", GetBody(ok).GetProperty("Data").GetProperty("fullName").GetString());
        }

        [Fact]
        public async Task UpdateProfile_EmptyFullName_DoesNotChangeFullName()
        {
            var user = NewUser();
            var originalName = user.FullName;
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _controller.UpdateProfile(new UpdateClientProfileDto { FullName = "" });

            Assert.Equal(originalName, user.FullName);
        }

        [Fact]
        public async Task UpdateProfile_UsernameTakenByAnotherUser_ReturnsBadRequest()
        {
            var user = NewUser();
            var otherUser = new AppUser { Id = Guid.NewGuid(), UserName = "takenname" };

            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.FindByNameAsync("takenname")).ReturnsAsync(otherUser);

            var result = await _controller.UpdateProfile(new UpdateClientProfileDto { FullName = "X", UserName = "takenname" });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Bu istifadəçi adı artıq mövcuddur.", GetBody(bad).GetProperty("Errors")[0].GetString());
            Assert.NotEqual("takenname", user.UserName);
        }

        [Fact]
        public async Task UpdateProfile_UsernameTakenBySameUser_AllowsUpdate()
        {
            var user = NewUser();
            user.UserName = "sameuser";

            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.UpdateProfile(new UpdateClientProfileDto { FullName = "X", UserName = "sameuser" });

            Assert.IsType<OkObjectResult>(result);
            _userManagerMock.Verify(m => m.FindByNameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateProfile_NewUniqueUsername_UpdatesSuccessfully()
        {
            var user = NewUser();
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.FindByNameAsync("newname")).ReturnsAsync((AppUser)null!);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.UpdateProfile(new UpdateClientProfileDto { FullName = "X", UserName = "newname" });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("newname", user.UserName);
            Assert.Equal("newname", GetBody(ok).GetProperty("Data").GetProperty("userName").GetString());
        }

        [Fact]
        public async Task UpdateProfile_UpdatesImageUrl()
        {
            var user = NewUser();
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.UpdateProfile(new UpdateClientProfileDto
            {
                FullName = "X",
                ImageUrl = "https://example.com/new.jpg"
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("https://example.com/new.jpg", user.ImageUrl);
            Assert.Equal("https://example.com/new.jpg", GetBody(ok).GetProperty("Data").GetProperty("imageUrl").GetString());
        }

        [Fact]
        public async Task UpdateProfile_EmptyImageUrl_DoesNotChangeImageUrl()
        {
            var user = NewUser();
            user.ImageUrl = "https://example.com/old.jpg";
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            await _controller.UpdateProfile(new UpdateClientProfileDto { FullName = "X", ImageUrl = "" });

            Assert.Equal("https://example.com/old.jpg", user.ImageUrl);
        }

        [Fact]
        public async Task UpdateProfile_UpdateAsyncFails_ReturnsBadRequest()
        {
            var user = NewUser();
            SetUser(_userId);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Concurrency failure." }));

            var result = await _controller.UpdateProfile(new UpdateClientProfileDto { FullName = "X" });

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Concurrency failure.", GetBody(bad).GetProperty("Errors")[0].GetString());
        }
    }
}