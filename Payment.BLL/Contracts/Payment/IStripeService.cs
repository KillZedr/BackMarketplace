using Payment.BLL.DTOs;
using Payment.Domain.DTOs;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Contracts.Payment
{
    public interface IStripeService : IService
    {
        Task<StripeList<Product>> GetAllStripeProductsAsync();
        Task<Product> GetStripeProductAsync(string id);

        Task<string> CreateStripeProductAsync(ProductCreationDto productDto);
        Task<string> CreateStripePriceAsync(string productId, decimal priceAmount);
        Task<string> CreateCheckoutSessionAsync(List<string> prices);
        Customer CreateStripeCustomer(UserDto userDto);

        Task<bool> DeleteStripeProductAsync(string productId);
        Task<bool> ArchiveStripeProductAsync(string productId);

        Task<bool> ActivateStripeProductAsync(string id);

        Task<Stripe.Product> UpdateStripeProductAsync(string id, ProductCreationDto productDto);
    }
}
