using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Payment.BLL.Services.PayProduct;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Payment.BLL.Services.Payment
{
    internal class StripeService : IStripeService
    {
        private readonly Stripe.ProductService _productService;
        private readonly PriceService _priceService;

        public StripeService()
        {
            _productService = new Stripe.ProductService();
            _priceService = new PriceService();
        }

        public async Task<StripeList<Product>> GetAllStripeProductsAsync()
        {
            return await _productService.ListAsync();
        }

        public async Task<Product> GetStripeProductAsync(string id)
        {
            return (await GetAllStripeProductsAsync()).First(p=>p.Id.Equals(id));
        }

        public async Task<(string ProductId, string PriceId)> CreateStripeProductAsync(ProductCreationDto productDto)
        {
            var productOptions = new ProductCreateOptions
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Metadata = new Dictionary<string, string>
                {
                    { "CategoryName", productDto.CategoryName }
                },
                Shippable = false
            };
            var product = await _productService.CreateAsync(productOptions);

            var priceOptions = new PriceCreateOptions
            {
                UnitAmount = (long?)(productDto.Price * 100),
                Currency = "eur",
                Product = product.Id,
            };
            var price = await _priceService.CreateAsync(priceOptions);

            return (product.Id, price.Id);
        }

        public async Task<Stripe.Product> UpdateStripeProductAsync(ProductCreationDto productDto)
        {
            var id = (await _productService.ListAsync()).First(p=>p.Name.Equals(productDto.Name)).Id;
            var productUpdateOptions = new ProductUpdateOptions 
            { 
                Description =  productDto.Description,
                Name = productDto.Name,
                Metadata = new Dictionary<string, string>
                {
                    { "CategoryName", productDto.CategoryName }
                }
            };
            var product = await _productService.UpdateAsync(id, productUpdateOptions);

            return product;
        }

        public async Task<bool> DeleteStripeProductAsync(string productId)
        {
            try
            {
                DeactivateProductPricesAsync(productId);
                var deletedProduct = await _productService.DeleteAsync(productId);//err
                return (bool)deletedProduct.Deleted;
            }
            catch (StripeException ex)
            {
                throw ex;
            }
        }

        private async Task DeactivateProductPricesAsync(string productId)
        {
            var prices = await _priceService.ListAsync(new PriceListOptions
            {
                Product = productId
            });
            foreach (var price in prices.Data)
            {
                if (price.Active)
                {
                    var updateOptions = new PriceUpdateOptions
                    {
                        Active = false
                    };
                    await _priceService.UpdateAsync(price.Id, updateOptions);
                }
            }
        }

        public async Task<bool> ArchiveStripeProductAsync(string productId)
        {
            try
            {
                var updateOptions = new ProductUpdateOptions
                {
                    Active = false
                };

                var updatedProduct = await _productService.UpdateAsync(productId, updateOptions);
                return !updatedProduct.Active;
            }
            catch (StripeException ex)
            {
                throw ex;
            }
        }
    }
}
