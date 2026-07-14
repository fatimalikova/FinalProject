using Stripe;

namespace AppointmentAPP.Interfaces
{
    public interface IStripeService
    {
        Task<Customer> CreateCustomerAsync(CustomerCreateOptions options);
        Task<PaymentIntent> CreatePaymentIntentAsync(PaymentIntentCreateOptions options);
        Task<SetupIntent> CreateSetupIntentAsync(SetupIntentCreateOptions options);
        Task<PaymentMethod> GetPaymentMethodAsync(string paymentMethodId);
    }
}
