using AppointmentAPP.Dtos.PaymentDtos;
using AppointmentAPP.Helpers;
using AppointmentAPP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly UserManager<AppUser> _userManager;

        public PaymentController(IConfiguration config, UserManager<AppUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        /// <summary>
        /// Cari istifadəçi üçün Stripe Customer-i qaytarır; yoxdursa yaradır və DB-də saxlayır.
        /// </summary>
        private async Task<string> GetOrCreateStripeCustomerAsync(AppUser user)
        {
            if (!string.IsNullOrWhiteSpace(user.StripeCustomerId))
                return user.StripeCustomerId;

            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.FullName,
                Metadata = new Dictionary<string, string>
                {
                    { "userId", user.Id.ToString() }
                }
            });

            user.StripeCustomerId = customer.Id;
            await _userManager.UpdateAsync(user);

            return customer.Id;
        }

        [Authorize]
        [HttpPost("create-intent")]
        public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentDto dto)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return NotFound(ResponseModelHelper.NotFoundResult<object>("İstifadəçi tapılmadı."));

            try
            {
                var customerId = await GetOrCreateStripeCustomerAsync(user);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(dto.Amount * 100),
                    Currency = "azn",
                    Description = dto.Description,
                    Customer = customerId,
                };

                if (!string.IsNullOrWhiteSpace(dto.PaymentMethodId))
                {
                    /* Saxlanılmış kartla ödəniş — Customer-ə artıq bağlıdır */
                    options.PaymentMethodTypes = new List<string> { "card" };
                    options.PaymentMethod = dto.PaymentMethodId;
                    options.Confirm = true;
                }
                else
                {
                    /* Yeni kartla ödəniş — frontend Stripe Elements ilə təsdiqləyəcək */
                    options.AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    };
                }

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                return Ok(ResponseModelHelper.SuccessResult(new
                {
                    clientSecret = intent.ClientSecret,
                    status = intent.Status
                }));
            }
            catch (StripeException ex)
            {
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(ex.Message));
            }
        }

        [Authorize]
        [HttpPost("setup-intent")]
        public async Task<IActionResult> CreateSetupIntent()
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return NotFound(ResponseModelHelper.NotFoundResult<object>("İstifadəçi tapılmadı."));

            try
            {
                var customerId = await GetOrCreateStripeCustomerAsync(user);

                var options = new SetupIntentCreateOptions
                {
                    Customer = customerId,
                    PaymentMethodTypes = new List<string> { "card" },
                    Usage = "off_session"
                };

                var service = new SetupIntentService();
                var setupIntent = await service.CreateAsync(options);

                return Ok(ResponseModelHelper.SuccessResult(new
                {
                    clientSecret = setupIntent.ClientSecret
                }));
            }
            catch (StripeException ex)
            {
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(ex.Message));
            }
        }

        [HttpGet("method/{pmId}")]
        public async Task<IActionResult> GetPaymentMethod(string pmId)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            try
            {
                var service = new PaymentMethodService();
                var pm = await service.GetAsync(pmId);

                if (pm?.Card == null)
                    return NotFound(ResponseModelHelper.NotFoundResult<object>("Kart tapılmadı"));

                return Ok(ResponseModelHelper.SuccessResult(new
                {
                    last4 = pm.Card.Last4,
                    brand = pm.Card.Brand,
                    expMonth = pm.Card.ExpMonth,
                    expYear = pm.Card.ExpYear
                }));
            }
            catch (StripeException ex)
            {
                return BadRequest(ResponseModelHelper.BadRequestResult<object>(ex.Message));
            }
        }
    }
}