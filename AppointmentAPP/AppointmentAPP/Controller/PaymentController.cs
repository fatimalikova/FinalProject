using AppointmentAPP.Dtos.PaymentDtos;
using AppointmentAPP.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace AppointmentAPP.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _config;

        public PaymentController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("create-intent")]
        public async Task<IActionResult> CreateIntent([FromBody] CreatePaymentIntentDto dto)
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(dto.Amount * 100),
                Currency = "azn",
                Description = dto.Description,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(options);

            return Ok(ResponseModelHelper.SuccessResult(new
            {
                clientSecret = intent.ClientSecret
            }));
        }

        [HttpPost("setup-intent")]
        public async Task<IActionResult> CreateSetupIntent()
        {
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];

            try
            {
                var options = new SetupIntentCreateOptions
                {
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