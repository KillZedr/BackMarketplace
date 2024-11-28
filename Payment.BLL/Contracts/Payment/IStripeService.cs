using Payment.BLL.DTOs;
using Payment.Domain.DTOs;
using Payment.Domain.ECommerce;
using Payment.Domain.Stripe;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Contracts.Payment;

public interface IStripeService : IService
{
    Task<StripeList<Product>> GetAllStripeProductsAsync();
    Task<Product> GetStripeProductAsync(string id);
    Task<string?> GetStripePriceIdByProductIdAsync(string stripeProductId);
    Task<Customer?> GetStripeCustomerAsync(string id);

    Task<string> CreateStripeProductAsync(ProductCreationDto productDto);
    Task<string> CreateStripePriceAsync(string productId, decimal priceAmount);
    Task<string> CreateCheckoutSessionAsync(List<string> prices, string customerId);
    Customer CreateStripeCustomer(UserDto userDto);
    Task<string?> CreateRefundAsync(string paymentIntentId, long amount, string reason);
    Task<string> CreateDonationAsync(decimal amount, string currency, string customerId);

    Task<bool?> DeleteStripeProductAsync(string productId);
    Task<bool?> ArchiveStripeProductAsync(string productId);

    Task<bool?> ActivateStripeProductAsync(string id);

    Task<Stripe.Product> UpdateStripeProductAsync(string id, ProductCreationDto productDto);

    // Новые методы для поддержки Google Pay и SEPA платежей
    Task<PaymentResultDto> ProcessSepaPaymentAsync(PaymentBasket basket, SepaPaymentRequestDto sepaRequest);
    Task<PaymentResultDto> ProcessGooglePayPaymentAsync(PaymentBasket basket, string googlePayToken);
    Task<PaymentResultDto> CreateGooglePayDonationAsync(decimal amount, string currency, string googlePayToken, string customerId);
    Task<PaymentResultDto> CreateSepaDonationAsync(SepaDonationRequestDto request, string customerId);
    PaymentFee ValidateAndPreparePaymentFee(PaymentFee fee);
    Task<PaymentFee?> GetPaymentFeeByMethodAsync(string paymentMethod);
}

