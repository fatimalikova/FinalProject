using AppointmentAPP.Controller;
using AppointmentAPP.Dtos.PaymentDtos;
using AppointmentAPP.Interfaces;
using AppointmentAPP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Stripe;
using Xunit;

namespace AppointmentAPP.Tests.Controllers
{
    public class PaymentControllerTest
    {
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IStripeService> _stripeMock = new();
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly PaymentController _controller;
        private readonly Guid _userId = Guid.NewGuid();

        public PaymentControllerTest()
        {
            _configMock.Setup(c => c["Stripe:SecretKey"]).Returns("sk_test_dummy");
            _userManagerMock = MockUserManager();

            _controller = new PaymentController(_configMock.Object, _userManagerMock.Object, _stripeMock.Object);
            SetControllerUser(_controller, _userId);
        }


        private static Mock<UserManager<AppUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<AppUser>>();
            return new Mock<UserManager<AppUser>>(
                store.Object, null, null, null, null, null, null, null, null);
        }

        private static void SetControllerUser(ControllerBase controller, Guid userId)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        private static JsonElement GetBody(IActionResult result)
        {
            var value = ((ObjectResult)result).Value;
            var json = JsonSerializer.Serialize(value);
            return JsonDocument.Parse(json).RootElement;
        }

        private AppUser NewUser(string? stripeCustomerId = null) => new()
        {
            Id = _userId,
            Email = "test@example.com",
            FullName = "Test User",
            StripeCustomerId = stripeCustomerId
        };


        [Fact]
        public async Task CreateIntent_UserNotFound_Returns404()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync((AppUser)null!);

            var result = await _controller.CreateIntent(new CreatePaymentIntentDto { Amount = 10 });

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var body = GetBody(notFound);
            Assert.False(body.GetProperty("Success").GetBoolean());
            Assert.Equal(404, body.GetProperty("StatusCode").GetInt32());
        }

        [Fact]
        public async Task CreateIntent_NoExistingCustomer_CreatesStripeCustomerAndPersistsId()
        {
            var user = NewUser(stripeCustomerId: null);
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

            _stripeMock.Setup(s => s.CreateCustomerAsync(It.IsAny<CustomerCreateOptions>()))
                .ReturnsAsync(new Customer { Id = "cus_new123" });
            _stripeMock.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>()))
                .ReturnsAsync(new PaymentIntent { ClientSecret = "pi_secret_1", Status = "requires_payment_method" });

            var result = await _controller.CreateIntent(new CreatePaymentIntentDto { Amount = 10, Description = "Test" });

            Assert.IsType<OkObjectResult>(result);
            _stripeMock.Verify(s => s.CreateCustomerAsync(It.IsAny<CustomerCreateOptions>()), Times.Once);
            _userManagerMock.Verify(m => m.UpdateAsync(It.Is<AppUser>(u => u.StripeCustomerId == "cus_new123")), Times.Once);
        }

        [Fact]
        public async Task CreateIntent_ExistingCustomer_DoesNotCreateNewCustomer()
        {
            var user = NewUser(stripeCustomerId: "cus_existing");
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _stripeMock.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>()))
                .ReturnsAsync(new PaymentIntent { ClientSecret = "pi_secret", Status = "succeeded" });

            await _controller.CreateIntent(new CreatePaymentIntentDto { Amount = 10 });

            _stripeMock.Verify(s => s.CreateCustomerAsync(It.IsAny<CustomerCreateOptions>()), Times.Never);
            _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<AppUser>()), Times.Never);
        }

        [Fact]
        public async Task CreateIntent_WithPaymentMethodId_ConfirmsImmediately()
        {
            var user = NewUser("cus_1");
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);

            PaymentIntentCreateOptions? captured = null;
            _stripeMock.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>()))
                .Callback<PaymentIntentCreateOptions>(o => captured = o)
                .ReturnsAsync(new PaymentIntent { ClientSecret = "pi_secret", Status = "succeeded" });

            await _controller.CreateIntent(new CreatePaymentIntentDto { Amount = 10, PaymentMethodId = "pm_123" });

            Assert.NotNull(captured);
            Assert.Equal("pm_123", captured!.PaymentMethod);
            Assert.True(captured.Confirm);
            Assert.Contains("card", captured.PaymentMethodTypes);
            Assert.Null(captured.AutomaticPaymentMethods);
        }

        [Fact]
        public async Task CreateIntent_WithoutPaymentMethodId_EnablesAutomaticPaymentMethods()
        {
            var user = NewUser("cus_1");
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);

            PaymentIntentCreateOptions? captured = null;
            _stripeMock.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>()))
                .Callback<PaymentIntentCreateOptions>(o => captured = o)
                .ReturnsAsync(new PaymentIntent { ClientSecret = "pi_secret", Status = "requires_payment_method" });

            await _controller.CreateIntent(new CreatePaymentIntentDto { Amount = 10 });

            Assert.NotNull(captured);
            Assert.True(captured!.AutomaticPaymentMethods!.Enabled);
            Assert.Null(captured.PaymentMethod);
            Assert.Null(captured.Confirm);
        }

        [Fact]
        public async Task CreateIntent_AmountIsConvertedToCents()
        {
            var user = NewUser("cus_1");
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);

            PaymentIntentCreateOptions? captured = null;
            _stripeMock.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>()))
                .Callback<PaymentIntentCreateOptions>(o => captured = o)
                .ReturnsAsync(new PaymentIntent { ClientSecret = "s", Status = "succeeded" });

            await _controller.CreateIntent(new CreatePaymentIntentDto { Amount = 12.34m });

            Assert.Equal(1234, captured!.Amount);
            Assert.Equal("azn", captured.Currency);
        }

        [Fact]
        public async Task CreateIntent_StripeExceptionThrown_Returns400WithMessage()
        {
            var user = NewUser("cus_1");
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _stripeMock.Setup(s => s.CreatePaymentIntentAsync(It.IsAny<PaymentIntentCreateOptions>()))
                .ThrowsAsync(new StripeException("Your card was declined."));

            var result = await _controller.CreateIntent(new CreatePaymentIntentDto { Amount = 10 });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var body = GetBody(badRequest);
            Assert.Equal("Your card was declined.", body.GetProperty("Errors")[0].GetString());
        }



        [Fact]
        public async Task CreateSetupIntent_UserNotFound_Returns404()
        {
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync((AppUser)null!);

            var result = await _controller.CreateSetupIntent();

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateSetupIntent_Success_ReturnsClientSecretWithCorrectOptions()
        {
            var user = NewUser("cus_1");
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);

            SetupIntentCreateOptions? captured = null;
            _stripeMock.Setup(s => s.CreateSetupIntentAsync(It.IsAny<SetupIntentCreateOptions>()))
                .Callback<SetupIntentCreateOptions>(o => captured = o)
                .ReturnsAsync(new SetupIntent { ClientSecret = "seti_secret" });

            var result = await _controller.CreateSetupIntent();

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = GetBody(ok);
            Assert.Equal("seti_secret", body.GetProperty("Data").GetProperty("clientSecret").GetString());

            Assert.Equal("off_session", captured!.Usage);
            Assert.Equal("cus_1", captured.Customer);
            Assert.Contains("card", captured.PaymentMethodTypes);
        }

        [Fact]
        public async Task CreateSetupIntent_ExistingCustomer_ReusesStripeCustomerId()
        {
            var user = NewUser("cus_existing");
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _stripeMock.Setup(s => s.CreateSetupIntentAsync(It.IsAny<SetupIntentCreateOptions>()))
                .ReturnsAsync(new SetupIntent { ClientSecret = "seti_x" });

            await _controller.CreateSetupIntent();

            _stripeMock.Verify(s => s.CreateCustomerAsync(It.IsAny<CustomerCreateOptions>()), Times.Never);
        }

        [Fact]
        public async Task CreateSetupIntent_StripeExceptionThrown_Returns400()
        {
            var user = NewUser("cus_1");
            _userManagerMock.Setup(m => m.FindByIdAsync(_userId.ToString())).ReturnsAsync(user);
            _stripeMock.Setup(s => s.CreateSetupIntentAsync(It.IsAny<SetupIntentCreateOptions>()))
                .ThrowsAsync(new StripeException("Invalid API key."));

            var result = await _controller.CreateSetupIntent();

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var body = GetBody(badRequest);
            Assert.Equal("Invalid API key.", body.GetProperty("Errors")[0].GetString());
        }


        [Fact]
        public async Task GetPaymentMethod_CardIsNull_Returns404()
        {
            _stripeMock.Setup(s => s.GetPaymentMethodAsync("pm_bank"))
                .ReturnsAsync(new PaymentMethod { Card = null });

            var result = await _controller.GetPaymentMethod("pm_bank");

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetPaymentMethod_Success_ReturnsCardDetails()
        {
            _stripeMock.Setup(s => s.GetPaymentMethodAsync("pm_123")).ReturnsAsync(new PaymentMethod
            {
                Card = new PaymentMethodCard { Brand = "visa", Last4 = "4242", ExpMonth = 12, ExpYear = 2030 }
            });

            var result = await _controller.GetPaymentMethod("pm_123");

            var ok = Assert.IsType<OkObjectResult>(result);
            var data = GetBody(ok).GetProperty("Data");
            Assert.Equal("visa", data.GetProperty("brand").GetString());
            Assert.Equal("4242", data.GetProperty("last4").GetString());
            Assert.Equal(12, data.GetProperty("expMonth").GetInt64());
            Assert.Equal(2030, data.GetProperty("expYear").GetInt64());
        }

        [Fact]
        public async Task GetPaymentMethod_StripeExceptionThrown_Returns400()
        {
            _stripeMock.Setup(s => s.GetPaymentMethodAsync(It.IsAny<string>()))
                .ThrowsAsync(new StripeException("No such payment_method"));

            var result = await _controller.GetPaymentMethod("pm_invalid");

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var body = GetBody(badRequest);
            Assert.Equal("No such payment_method", body.GetProperty("Errors")[0].GetString());
        }

        [Fact]
        public async Task GetPaymentMethod_PassesCorrectIdToStripeService()
        {
            _stripeMock.Setup(s => s.GetPaymentMethodAsync("pm_specific"))
                .ReturnsAsync(new PaymentMethod { Card = new PaymentMethodCard { Brand = "mastercard", Last4 = "0000", ExpMonth = 1, ExpYear = 2027 } });

            await _controller.GetPaymentMethod("pm_specific");

            _stripeMock.Verify(s => s.GetPaymentMethodAsync("pm_specific"), Times.Once);
        }
    }
}