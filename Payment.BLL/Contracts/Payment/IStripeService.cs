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
        Task<(string ProductId, string PriceId)> CreateStripeProductAsync(ProductCreationDto productDto);
        Task<bool> DeleteStripeProductAsync(string productId);
        Task<StripeList<Product>> GetAllStripeProductsAsync();
        Task<bool> ArchiveStripeProductAsync(string productId);
        Task<Stripe.Product> UpdateStripeProductAsync(ProductCreationDto productDto);
        Task<Product> GetStripeProductAsync(string id);
    }
}
