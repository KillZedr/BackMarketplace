using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Payment.Domain.ECommerce;
using Stripe;
using Stripe.Checkout;
using Stripe.V2;

namespace Paymant_Module_NEOXONLINE.Controllers.Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripeService;

        public StripeController(IStripeService stripeService)
        {
            _stripeService = stripeService;
        }

        [HttpGet("GetAllStripeProducts")]
        public async Task<IActionResult> GetAllStripeProducts()
        {
            try
            {
                var products = await _stripeService.GetAllStripeProductsAsync();
                if (products != null)
                {
                    return Ok(products);
                }
                return NotFound("there aren't any products");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("GetStripeProduct")]
        public async Task<IActionResult> GetStripeProduct(string id)
        {
            try
            {
                var product = await _stripeService.GetStripeProductAsync(id);
                if (product != null)
                {
                    return Ok(product);
                }
                return NotFound($"product with id {id} does not exist");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("CreateProduct")]
        public async Task<IActionResult> CreateProduct(ProductCreationDto productDto)
        {
            try
            {
                var productId = await _stripeService.CreateStripeProductAsync(productDto);
                var priceId = await _stripeService.CreateStripePriceAsync(productId, productDto.Price);
                return Ok(new {ProductId = productId, PriceId = priceId});
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("CreateCheckoutSession")]
        public async Task<IActionResult> CreateCheckoutSession(List<string> prices)
        {
            try
            {
                return Ok(await _stripeService.CreateCheckoutSessionAsync(prices));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("CreateStripeCustomer")]
        public async Task<IActionResult> CreateStripeCustomer(UserDto userDto)
        {
            try
            {
                return Ok(_stripeService.CreateStripeCustomer(userDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct(string id, ProductCreationDto productDto)
        {
            try
            {
               return Ok(await _stripeService.UpdateStripeProductAsync(id, productDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("ArchiveProduct")]
        public async Task<IActionResult> ArchiveProduct(string productId)
        {
            try
            {                
                return Ok(await _stripeService.ArchiveStripeProductAsync(productId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }            
        }

        [HttpDelete("DeleteProduct")]
        public async Task<IActionResult> DeleteProduct(string productId)
        {
            try
            {
                return Ok(await _stripeService.DeleteStripeProductAsync(productId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


    }
}
