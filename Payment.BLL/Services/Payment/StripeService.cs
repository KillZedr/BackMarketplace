using Microsoft.AspNetCore.Mvc;
using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Payment.BLL.Services.PayProduct;
using Payment.Domain.Identity;
using Stripe;
using Stripe.Checkout;
using Stripe.Forwarding;
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
        private readonly SessionService _sessionService;
        private readonly CustomerService _customerService;

        public StripeService()
        {
            _productService = new Stripe.ProductService();
            _priceService = new PriceService();
            _sessionService = new SessionService();
            _customerService = new CustomerService();
        }

        public async Task<StripeList<Product>> GetAllStripeProductsAsync()
        {
            return await _productService.ListAsync();
        }

        public async Task<Product> GetStripeProductAsync(string id)
        {
            return await _productService.GetAsync(id);
        }

        public async Task<string> CreateStripeProductAsync(ProductCreationDto productDto)
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

            return product.Id;
        }

        public async Task<string> CreateStripePriceAsync(string productId, decimal priceAmount)
        {
            var priceOptions = new PriceCreateOptions
            {
                UnitAmount = (long?)(priceAmount * 100),
                Currency = "eur",
                Product = productId,
            };
            var price = await _priceService.CreateAsync(priceOptions);

            return price.Id;
        }

        public async Task<Stripe.Product> UpdateStripeProductAsync(string id, ProductCreationDto productDto)
        {
            var productUpdateOptions = new ProductUpdateOptions 
            { 
                Name = productDto.Name,
                Description =  productDto.Description,
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

        public async Task<bool> ArchiveStripeProductAsync(string id)
        {
            try
            {
                var updateOptions = new ProductUpdateOptions
                {
                    Active = false
                };

                var updatedProduct = await _productService.UpdateAsync(id, updateOptions);
                return !updatedProduct.Active;
            }
            catch (StripeException ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ActivateStripeProductAsync(string id)
        {
            var updateOptions = new ProductUpdateOptions
            {
                Active = true
            };

            var updatedProduct = await _productService.UpdateAsync(id, updateOptions);
            return updatedProduct.Active;
        }

        public async Task<string> CreateCheckoutSessionAsync(List<string> prices)
        {
            var lineItems = prices.Select(product => new SessionLineItemOptions
            {
                Price = product,
                Quantity = 1,
            }).ToList();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", "paypal", "sepa_debit"},
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = "https://your-website.com/success?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = "https://your-website.com/cancel",
            };
            Session session = await _sessionService.CreateAsync(options);            
            return session.Url;
        }

        public Customer CreateStripeCustomer(UserDto userDto)
        {            
            var customerOptions = new CustomerCreateOptions
            {
                Email = userDto.Email,
                Name = userDto.Name,
                Phone = userDto.PhoneNumber,
                Address = new AddressOptions
                {
                    Line1 = userDto.Address,
                    City = userDto.City,
                    Country = userDto.Сountry
                },                
            };
            var customer = _customerService.Create(customerOptions);

            return customer;
        }
    }
}
