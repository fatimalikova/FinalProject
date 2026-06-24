using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.Login_RegisterDtos;
using AppointmentAPP.Dtos.UserDtos;
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
using Xunit;

namespace AppointmentAPP.Tests.Controllers;

    public class AccountControllerTest
    {
        private readonly Mock<IValidator<RegisterDto>> _registerValidator = new();
        private readonly Mock<IValidator<LoginDto>> _loginValidator = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<UserManager<AppUser>> _userManager;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManager;
        private readonly Mock<IConfiguration> _config = new();
        private readonly Mock<IJwtService> _jwtService = new();
        private readonly Mock<IEmailService> _emailService = new();

        private readonly AccountController _controller;

        public AccountControllerTest()
        {
            _userManager = MockUserManager<AppUser>();
            _roleManager = MockRoleManager<IdentityRole<Guid>>();

            // "await null" -> NullReferenceException olmasın deyə default Task qaytarırıq.
            _emailService
                .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _controller = new AccountController(
                _registerValidator.Object,
                _loginValidator.Object,
                _mapper.Object,
                _userManager.Object,
                _roleManager.Object,
                _config.Object,
                _jwtService.Object,
                _emailService.Object);

            // Register endpoint Request.Scheme/Host istifadə edir -> HttpContext lazımdır.
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        // ----------------------- Köməkçi metodlar -----------------------

        private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            return new Mock<UserManager<TUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
        {
            var store = new Mock<IRoleStore<TRole>>();
            var validators = new List<IRoleValidator<TRole>> { new RoleValidator<TRole>() };
            return new Mock<RoleManager<TRole>>(store.Object, validators, null!, null!, null!);
        }

        private static ValidationResult Valid() => new();
        private static ValidationResult Invalid(string msg) =>
            new(new[] { new ValidationFailure("field", msg) });

        private void SetUserClaims(Guid userId, string userName = "ali", string? fullName = null, params string[] roles)
        {
            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, userName),
        };
            if (fullName is not null) claims.Add(new Claim("FullName", fullName));
            foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

            _controller.ControllerContext.HttpContext.User =
                new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        // =====================================================================
        //  REGISTER  (RegisterInternal-ın bütün branch-ları)
        // =====================================================================

        [Fact]
        public async Task RegisterClient_InvalidModel_ReturnsBadRequest()
        {
            var dto = new RegisterDto();
            _registerValidator.Setup(v => v.Validate(dto)).Returns(Invalid("UserName boş ola bilməz"));

            var result = await _controller.RegisterClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterClient_UserAlreadyExists_ReturnsBadRequest()
        {
            var dto = new RegisterDto { UserName = "ali", Email = "ali@mail.com", Password = "Pass123!" };
            _registerValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(new AppUser());

            var result = await _controller.RegisterClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterClient_EmailAlreadyExists_ReturnsBadRequest()
        {
            var dto = new RegisterDto { UserName = "ali", Email = "ali@mail.com", Password = "Pass123!" };
            _registerValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser?)null);
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(new AppUser());

            var result = await _controller.RegisterClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task RegisterClient_CreateFails_ReturnsBadRequest_AndDoesNotAddRole()
        {
            var dto = new RegisterDto { UserName = "ali", Email = "ali@mail.com", Password = "weak" };
            var appUser = new AppUser { UserName = dto.UserName, Email = dto.Email };
            _registerValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser?)null);
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser?)null);
            _mapper.Setup(m => m.Map<AppUser>(dto)).Returns(appUser);
            _userManager.Setup(u => u.CreateAsync(appUser, dto.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "şifrə zəifdir" }));

            var result = await _controller.RegisterClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
            _userManager.Verify(u => u.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterClient_AddToRoleFails_DeletesUser_ReturnsBadRequest()
        {
            var dto = new RegisterDto { UserName = "ali", Email = "ali@mail.com", Password = "Pass123!" };
            var appUser = new AppUser { UserName = dto.UserName, Email = dto.Email };
            _registerValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser?)null);
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser?)null);
            _mapper.Setup(m => m.Map<AppUser>(dto)).Returns(appUser);
            _userManager.Setup(u => u.CreateAsync(appUser, dto.Password)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(u => u.AddToRoleAsync(appUser, "Client"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "role error" }));
            _userManager.Setup(u => u.DeleteAsync(appUser)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.RegisterClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
            _userManager.Verify(u => u.DeleteAsync(appUser), Times.Once); // yarımçıq user silinməlidir
        }

        [Fact]
        public async Task RegisterClient_ValidData_ReturnsOk_AndSendsConfirmationEmail()
        {
            var dto = new RegisterDto { UserName = "ali", Email = "ali@mail.com", Password = "Pass123!" };
            var appUser = new AppUser { UserName = dto.UserName, Email = dto.Email };
            _registerValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser?)null);
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser?)null);
            _mapper.Setup(m => m.Map<AppUser>(dto)).Returns(appUser);
            _userManager.Setup(u => u.CreateAsync(appUser, dto.Password)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(u => u.AddToRoleAsync(appUser, "Client")).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(u => u.GenerateEmailConfirmationTokenAsync(appUser)).ReturnsAsync("token123");

            var result = await _controller.RegisterClient(dto);

            Assert.IsType<OkObjectResult>(result);
            _userManager.Verify(u => u.AddToRoleAsync(appUser, "Client"), Times.Once);
            _emailService.Verify(e => e.SendEmailAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RegisterProvider_ValidData_AddsProviderRole_ReturnsOk()
        {
            var dto = new RegisterDto { UserName = "dr", Email = "dr@mail.com", Password = "Pass123!" };
            var appUser = new AppUser { UserName = dto.UserName, Email = dto.Email };
            _registerValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser?)null);
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser?)null);
            _mapper.Setup(m => m.Map<AppUser>(dto)).Returns(appUser);
            _userManager.Setup(u => u.CreateAsync(appUser, dto.Password)).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(u => u.AddToRoleAsync(appUser, "Provider")).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(u => u.GenerateEmailConfirmationTokenAsync(appUser)).ReturnsAsync("token");

            var result = await _controller.RegisterProvider(dto);

            Assert.IsType<OkObjectResult>(result);
            _userManager.Verify(u => u.AddToRoleAsync(appUser, "Provider"), Times.Once);
        }

        // =====================================================================
        //  LOGIN  (LoginInternal-ın bütün branch-ları + 3 portalın hamısı)
        // =====================================================================

        [Fact]
        public async Task LoginClient_InvalidModel_ReturnsBadRequest()
        {
            var dto = new LoginDto();
            _loginValidator.Setup(v => v.Validate(dto)).Returns(Invalid("UserName boş ola bilməz"));

            var result = await _controller.LoginClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LoginClient_UserNotFound_ReturnsBadRequest()
        {
            var dto = new LoginDto { UserName = "ali", Password = "Pass123!" };
            _loginValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser?)null);

            var result = await _controller.LoginClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LoginClient_WrongPassword_ReturnsBadRequest()
        {
            var dto = new LoginDto { UserName = "ali", Password = "wrong" };
            var user = new AppUser { UserName = "ali", EmailConfirmed = true };
            _loginValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(false);

            var result = await _controller.LoginClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LoginClient_EmailNotConfirmed_ReturnsBadRequest()
        {
            var dto = new LoginDto { UserName = "ali", Password = "Pass123!" };
            var user = new AppUser { UserName = "ali", EmailConfirmed = false };
            _loginValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);

            var result = await _controller.LoginClient(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LoginClient_WrongPortal_RoleMismatch_ReturnsBadRequest()
        {
            var dto = new LoginDto { UserName = "ali", Password = "Pass123!" };
            var user = new AppUser { UserName = "ali", Email = "ali@mail.com", EmailConfirmed = true };
            _loginValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Provider" });

            var result = await _controller.LoginClient(dto); // Client portalı, amma user Provider-dir

            Assert.IsType<BadRequestObjectResult>(result);
            _userManager.Verify(u => u.UpdateAsync(It.IsAny<AppUser>()), Times.Never); // 2FA kodu yazılmamalıdır
        }

        [Fact]
        public async Task LoginClient_ValidData_Sends2FA_ReturnsOk()
        {
            var dto = new LoginDto { UserName = "ali", Password = "Pass123!" };
            var user = new AppUser { UserName = "ali", Email = "ali@mail.com", EmailConfirmed = true };
            _loginValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.LoginClient(dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(user.TwoFactorCode);
            Assert.NotNull(user.TwoFactorCodeExpiry);
            _emailService.Verify(e => e.SendEmailAsync(user.Email!, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task LoginProvider_ValidData_Sends2FA_ReturnsOk()
        {
            var dto = new LoginDto { UserName = "dr", Password = "Pass123!" };
            var user = new AppUser { UserName = "dr", Email = "dr@mail.com", EmailConfirmed = true };
            _loginValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Provider" });
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.LoginProvider(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task LoginAdmin_ValidData_Sends2FA_ReturnsOk()
        {
            var dto = new LoginDto { UserName = "admin", Password = "Pass123!" };
            var user = new AppUser { UserName = "admin", Email = "admin@mail.com", EmailConfirmed = true };
            _loginValidator.Setup(v => v.Validate(dto)).Returns(Valid());
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManager.Setup(u => u.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Admin" });
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.LoginAdmin(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        // =====================================================================
        //  VERIFY 2FA  (3 ayrı BadRequest səbəbi: yanlış kod / expiry null / vaxt keçib)
        // =====================================================================

        [Fact]
        public async Task Verify2FA_UserNotFound_ReturnsNotFound()
        {
            var dto = new Verify2FADto { UserName = "ali", Code = "123456" };
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync((AppUser?)null);

            var result = await _controller.Verify2FA(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Verify2FA_WrongCode_ReturnsBadRequest()
        {
            var dto = new Verify2FADto { UserName = "ali", Code = "000000" };
            var user = new AppUser
            {
                UserName = "ali",
                TwoFactorCode = "123456",
                TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5)
            };
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);

            var result = await _controller.Verify2FA(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Verify2FA_NullExpiry_ReturnsBadRequest()
        {
            var dto = new Verify2FADto { UserName = "ali", Code = "123456" };
            var user = new AppUser { UserName = "ali", TwoFactorCode = "123456", TwoFactorCodeExpiry = null };
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);

            var result = await _controller.Verify2FA(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Verify2FA_ExpiredCode_ReturnsBadRequest()
        {
            var dto = new Verify2FADto { UserName = "ali", Code = "123456" };
            var user = new AppUser
            {
                UserName = "ali",
                TwoFactorCode = "123456",
                TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(-1) // vaxtı keçib
            };
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);

            var result = await _controller.Verify2FA(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Verify2FA_ValidCode_ReturnsOk_ClearsCode_SetsRefreshToken()
        {
            var dto = new Verify2FADto { UserName = "ali", Code = "123456" };
            var user = new AppUser
            {
                UserName = "ali",
                TwoFactorCode = "123456",
                TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5)
            };
            _userManager.Setup(u => u.FindByNameAsync(dto.UserName)).ReturnsAsync(user);
            _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });
            _jwtService.Setup(j => j.GenerateToken(
                It.IsAny<AppUser>(), It.IsAny<IList<string>>(), It.IsAny<IConfiguration>())).Returns("access-token");
            _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Verify2FA(dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.Null(user.TwoFactorCode);
            Assert.Null(user.TwoFactorCodeExpiry);
            Assert.Equal("refresh-token", user.RefreshToken);
            Assert.NotNull(user.RefreshTokenExpiry);
        }

        // =====================================================================
        //  CONFIRM EMAIL
        // =====================================================================

        [Fact]
        public async Task ConfirmEmail_UserNotFound_ReturnsNotFound()
        {
            _userManager.Setup(u => u.FindByEmailAsync("yox@mail.com")).ReturnsAsync((AppUser?)null);

            var result = await _controller.ConfirmEmail("yox@mail.com", "token");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmEmail_InvalidToken_ReturnsBadRequest()
        {
            var user = new AppUser { Email = "ali@mail.com" };
            _userManager.Setup(u => u.FindByEmailAsync("ali@mail.com")).ReturnsAsync(user);
            _userManager.Setup(u => u.ConfirmEmailAsync(user, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "invalid token" }));

            var result = await _controller.ConfirmEmail("ali@mail.com", "bad-token");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmEmail_ValidToken_ReturnsOk()
        {
            var user = new AppUser { Email = "ali@mail.com" };
            _userManager.Setup(u => u.FindByEmailAsync("ali@mail.com")).ReturnsAsync(user);
            _userManager.Setup(u => u.ConfirmEmailAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ConfirmEmail("ali@mail.com", "token");

            Assert.IsType<OkObjectResult>(result);
        }

        // =====================================================================
        //  FORGOT PASSWORD
        // =====================================================================

        [Fact]
        public async Task ForgotPassword_UserNotFound_ReturnsNotFound()
        {
            var dto = new ForgotPasswordDto { Email = "yox@mail.com" };
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser?)null);

            var result = await _controller.ForgotPassword(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ForgotPassword_UserExists_ReturnsOk_AndSendsEmail()
        {
            var dto = new ForgotPasswordDto { Email = "ali@mail.com" };
            var user = new AppUser { Email = dto.Email };
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManager.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

            var result = await _controller.ForgotPassword(dto);

            Assert.IsType<OkObjectResult>(result);
            _emailService.Verify(e => e.SendEmailAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // =====================================================================
        //  RESET PASSWORD
        // =====================================================================

        [Fact]
        public async Task ResetPassword_UserNotFound_ReturnsNotFound()
        {
            var dto = new ResetPasswordDto { Email = "yox@mail.com", Token = "t", NewPassword = "New123!" };
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync((AppUser?)null);

            var result = await _controller.ResetPassword(dto);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
        {
            var dto = new ResetPasswordDto { Email = "ali@mail.com", Token = "bad", NewPassword = "New123!" };
            var user = new AppUser { Email = dto.Email };
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManager.Setup(u => u.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "invalid token" }));

            var result = await _controller.ResetPassword(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ValidToken_ReturnsOk()
        {
            var dto = new ResetPasswordDto { Email = "ali@mail.com", Token = "t", NewPassword = "New123!" };
            var user = new AppUser { Email = dto.Email };
            _userManager.Setup(u => u.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManager.Setup(u => u.ResetPasswordAsync(user, dto.Token, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ResetPassword(dto);

            Assert.IsType<OkObjectResult>(result);
        }

        // =====================================================================
        //  REFRESH  (BadRequest + 4 ayrı Unauthorized səbəbi + uğurlu)
        // =====================================================================

        [Fact]
        public async Task Refresh_InvalidAccessToken_ReturnsBadRequest()
        {
            var dto = new RefreshTokenDto { AccessToken = "bad", RefreshToken = "r" };
            _jwtService.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, It.IsAny<IConfiguration>()))
                .Returns((ClaimsPrincipal?)null);

            var result = await _controller.Refresh(dto);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_PrincipalWithoutUserId_ReturnsUnauthorized()
        {
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "r" };
            // NameIdentifier claim-i yoxdur -> userId null -> user null
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "ali") }, "TestAuth"));
            _jwtService.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, It.IsAny<IConfiguration>()))
                .Returns(principal);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
            _userManager.Verify(u => u.FindByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Refresh_UserNotFound_ReturnsUnauthorized()
        {
            var userId = Guid.NewGuid();
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "r" };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"));
            _jwtService.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, It.IsAny<IConfiguration>()))
                .Returns(principal);
            _userManager.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync((AppUser?)null);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_RefreshTokenMismatch_ReturnsUnauthorized()
        {
            var userId = Guid.NewGuid();
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "client-token" };
            var user = new AppUser
            {
                Id = userId,
                RefreshToken = "server-token", // fərqlidir
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"));
            _jwtService.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, It.IsAny<IConfiguration>()))
                .Returns(principal);
            _userManager.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_NullRefreshTokenExpiry_ReturnsUnauthorized()
        {
            var userId = Guid.NewGuid();
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "valid" };
            var user = new AppUser { Id = userId, RefreshToken = "valid", RefreshTokenExpiry = null };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"));
            _jwtService.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, It.IsAny<IConfiguration>()))
                .Returns(principal);
            _userManager.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ExpiredRefreshToken_ReturnsUnauthorized()
        {
            var userId = Guid.NewGuid();
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "valid" };
            var user = new AppUser
            {
                Id = userId,
                RefreshToken = "valid",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1) // vaxtı keçib
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"));
            _jwtService.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, It.IsAny<IConfiguration>()))
                .Returns(principal);
            _userManager.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            var result = await _controller.Refresh(dto);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_Valid_ReturnsOk_WithNewTokens()
        {
            var userId = Guid.NewGuid();
            var dto = new RefreshTokenDto { AccessToken = "expired", RefreshToken = "valid-refresh" };
            var user = new AppUser
            {
                Id = userId,
                RefreshToken = "valid-refresh",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth"));
            _jwtService.Setup(j => j.GetPrincipalFromExpiredToken(dto.AccessToken, It.IsAny<IConfiguration>()))
                .Returns(principal);
            _userManager.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManager.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });
            _jwtService.Setup(j => j.GenerateToken(
                It.IsAny<AppUser>(), It.IsAny<IList<string>>(), It.IsAny<IConfiguration>())).Returns("new-access");
            _jwtService.Setup(j => j.GenerateRefreshToken()).Returns("new-refresh");
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Refresh(dto);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("new-refresh", user.RefreshToken);
        }

        // =====================================================================
        //  PROFILE
        // =====================================================================

        [Fact]
        public void Profile_WithClaims_ReturnsOk()
        {
            SetUserClaims(Guid.NewGuid(), "ali", "Ali Aliyev", "Client");

            var result = _controller.Profile();

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void Profile_NoClaims_ReturnsOk()
        {
            // Heç bir claim təyin edilməyib -> nullable sahələr null qayıdır, amma yenə Ok
            var result = _controller.Profile();

            Assert.IsType<OkObjectResult>(result);
        }

        // =====================================================================
        //  LOGOUT  (user var / user yoxdur / userId claim yoxdur)
        // =====================================================================

        [Fact]
        public async Task Logout_UserExists_ClearsRefreshToken_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            var user = new AppUser { Id = userId, RefreshToken = "old", RefreshTokenExpiry = DateTime.UtcNow.AddDays(1) };
            SetUserClaims(userId, "ali");
            _userManager.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _userManager.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Logout();

            Assert.IsType<OkObjectResult>(result);
            Assert.Null(user.RefreshToken);
            Assert.Null(user.RefreshTokenExpiry);
        }

        [Fact]
        public async Task Logout_UserNotFound_ReturnsOk_WithoutUpdate()
        {
            var userId = Guid.NewGuid();
            SetUserClaims(userId, "ali");
            _userManager.Setup(u => u.FindByIdAsync(userId.ToString())).ReturnsAsync((AppUser?)null);

            var result = await _controller.Logout();

            Assert.IsType<OkObjectResult>(result);
            _userManager.Verify(u => u.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task Logout_NoUserIdClaim_ReturnsOk_WithoutLookup()
        {
            // User claim-ləri təyin edilməyib -> userId null -> FindByIdAsync çağırılmır
            var result = await _controller.Logout();

            Assert.IsType<OkObjectResult>(result);
            _userManager.Verify(u => u.FindByIdAsync(It.IsAny<string>()), Times.Never);
            _userManager.Verify(u => u.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }
    }
