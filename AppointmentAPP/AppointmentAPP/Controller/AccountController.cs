using AppointmentAPP.Dtos.Login_RegisterDtos;
using AppointmentAPP.Dtos.UserDtos;
using AppointmentAPP.Helpers;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        IMapper mapper,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration config,
        IJwtService jwtService,
        IEmailService emailService
        ) : ControllerBase
    {

        //[HttpGet("create-roles")]
        //public async Task<IActionResult> CreateRoles()
        //{
        //    string[] roles = { "Admin", "Provider", "Client" };

        //    foreach (var role in roles)
        //    {
        //        var exists = await roleManager.RoleExistsAsync(role);
        //        if (!exists)
        //            await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        //    }

        //    return Ok(ResponseModelHelper.SuccessResult("Roles created successfully."));
        //}



        [HttpPost("register/client")]
        public Task<IActionResult> RegisterClient([FromBody] RegisterDto registerDto)
    => RegisterInternal(registerDto, "Client");

        [HttpPost("register/provider")]
        public Task<IActionResult> RegisterProvider([FromBody] RegisterDto registerDto)
            => RegisterInternal(registerDto, "Provider");

        private async Task<IActionResult> RegisterInternal(RegisterDto registerDto, string role)
        {
            var validationResult = registerValidator.Validate(registerDto);
            if (!validationResult.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray()));

            var existingUser = await userManager.FindByNameAsync(registerDto.UserName);
            if (existingUser is not null)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>("User already exists"));

            var existingEmail = await userManager.FindByEmailAsync(registerDto.Email);
            if (existingEmail is not null)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>("Email is already registered"));

            var user = mapper.Map<AppUser>(registerDto);

            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    result.Errors.Select(e => e.Description).ToArray()));

            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user); // yarımçıq user qalmasın
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    roleResult.Errors.Select(e => e.Description).ToArray()));
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = $"{Request.Scheme}://{Request.Host}/api/account/confirm-email" +
                               $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            await emailService.SendEmailAsync(user.Email!, "Email Confirmation",
                $"<h3>Email-inizi təsdiqləyin</h3><p>Aşağıdakı linkə klikləyin:</p><a href='{confirmLink}'>Təsdiqlə</a>");

            return Ok(ResponseModelHelper.SuccessResult("Qeydiyyat uğurlu oldu. Email-inizi yoxlayın."));
        }


        [HttpPost("login/client")]
        public Task<IActionResult> LoginClient([FromBody] LoginDto loginDto)
             => LoginInternal(loginDto, "Client");

        [HttpPost("login/provider")]
        public Task<IActionResult> LoginProvider([FromBody] LoginDto loginDto)
            => LoginInternal(loginDto, "Provider");

        [HttpPost("login/admin")]
        public Task<IActionResult> LoginAdmin([FromBody] LoginDto loginDto)
            => LoginInternal(loginDto, "Admin");

        private async Task<IActionResult> LoginInternal(LoginDto loginDto, string requiredRole)
        {
            var validationResult = loginValidator.Validate(loginDto);
            if (!validationResult.IsValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray()));

            var user = await userManager.FindByNameAsync(loginDto.UserName);
            if (user is null)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>("Invalid username or password"));

            var passwordValid = await userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!passwordValid)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>("Invalid username or password"));

            if (!user.EmailConfirmed)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>("Email is not confirmed."));

            // 🔑 Əsas məhdudiyyət — bu hesab bu giriş qapısından girə bilər mi?
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains(requiredRole))
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    $"This account cannot log in from the {requiredRole} portal."));

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            user.TwoFactorCode = code;
            user.TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(5);
            await userManager.UpdateAsync(user);

            await emailService.SendEmailAsync(user.Email!, "2FA Kodu",
                $"<h3>Giriş doğrulama kodu</h3><p>Kodunuz: <b>{code}</b></p><p>Bu kod 5 dəqiqə ərzində etibarlıdır.</p>");

            return Ok(ResponseModelHelper.SuccessResult("2FA kodu email-inizə göndərildi."));
        }





        [HttpPost("verify-2fa")]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
        {
            var user = await userManager.FindByNameAsync(dto.UserName);
            if (user is null)
                return NotFound(ResponseModelHelper.NotFoundResult<object>("User not found"));

            if (user.TwoFactorCode != dto.Code || user.TwoFactorCodeExpiry is null || user.TwoFactorCodeExpiry < DateTime.UtcNow)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>("Kod yanlışdır və ya vaxtı keçib."));

            var roles = await userManager.GetRolesAsync(user);
            var accessToken = jwtService.GenerateToken(user, roles, config);
            var refreshToken = jwtService.GenerateRefreshToken();

            user.TwoFactorCode = null;
            user.TwoFactorCodeExpiry = null;
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(user);

            return Ok(ResponseModelHelper.SuccessResult(new { accessToken, refreshToken }));
        }





        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string email, [FromQuery] string token)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                return NotFound(ResponseModelHelper.NotFoundResult<object>("User not found"));

            var decodedToken = Uri.UnescapeDataString(token);

            var result = await userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    result.Errors.Select(e => e.Description).ToArray()));

            return Ok(ResponseModelHelper.SuccessResult("Email uğurla təsdiqləndi!"));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return NotFound(ResponseModelHelper.NotFoundResult<object>("User not found"));

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            await emailService.SendEmailAsync(dto.Email, "Reset Password",
                $"<h3>Şifrə sıfırlama</h3><p>Email: <b>{dto.Email}</b></p>" +
                $"<p>Token:</p><p style='word-break:break-all'><b>{token}</b></p>" +
                $"<p>Bu tokeni Swagger-də reset-password endpoint-ə yapışdırın.</p>");

            return Ok(ResponseModelHelper.SuccessResult("Şifrə sıfırlama tokeni email-inizə göndərildi."));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return NotFound(ResponseModelHelper.NotFoundResult<object>("User not found"));

            var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(
                    result.Errors.Select(e => e.Description).ToArray()));

            return Ok(ResponseModelHelper.SuccessResult("Şifrə uğurla dəyişdirildi!"));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            var principal = jwtService.GetPrincipalFromExpiredToken(dto.AccessToken, config);
            if (principal is null)
                return BadRequest(ResponseModelHelper.BadRequestResult<object>("Invalid access token"));

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);

            if (user is null ||
                user.RefreshToken != dto.RefreshToken ||
                user.RefreshTokenExpiry is null ||
                user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized(ResponseModelHelper.UnauthorizedResult<object>("Refresh token is invalid or expired"));

            var roles = await userManager.GetRolesAsync(user);
            var newAccessToken = jwtService.GenerateToken(user, roles, config);
            var newRefreshToken = jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(user);

            return Ok(ResponseModelHelper.SuccessResult(new { accessToken = newAccessToken, refreshToken = newRefreshToken }));
        }

        [HttpGet("profile")]
        [Authorize]
        public IActionResult Profile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.Identity?.Name;
            var fullName = User.FindFirst("FullName")?.Value;
            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            return Ok(ResponseModelHelper.SuccessResult(new { userId, userName, fullName, roles }));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);

            if (user is not null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await userManager.UpdateAsync(user);
            }

            return Ok(ResponseModelHelper.SuccessResult("Çıxış edildi."));
        }
    }
}
