using AppointmentAPP.Interfaces;
using Stripe;

namespace AppointmentAPP.Services
{
    public class StripeService : IStripeService
    {
        public Task<Customer> CreateCustomerAsync(CustomerCreateOptions options) => new CustomerService().CreateAsync(options);

        public Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options) => new PaymentIntentService().CreateAsync(options);

        public Task<SetupIntent> CreateSetupIntentAsync(SetupIntentCreateOptions options) => new SetupIntentService().CreateAsync(options);

        public Task<PaymentMethod> GetPaymentMethodAsync(string paymentMethodId) => new PaymentMethodService().GetAsync(paymentMethodId);
    }
}
