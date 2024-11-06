using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly string _webhookSecret;


        public StripeController(IStripeService stripeService, IConfiguration configuration)
        {
            _stripeService = stripeService;
            _webhookSecret = configuration["Stripe:WebhookSecret"];
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
        public async Task<IActionResult> CreateStripeCustomer1(UserDto userDto)
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

        [HttpPatch("ActivateProduct")]
        public async Task<IActionResult> ActivateProduct(string id)
        {
            try
            {
                return Ok(await _stripeService.ActivateStripeProductAsync(id));
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

        [HttpPost("StripeWebhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _webhookSecret,
                    300,
                    false
                );
                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;

                    if(session.Status == "complete")
                    {
                        Console.WriteLine($"Checkout completed for session: {session.Id}");
                    }
                    else
                    {
                        Console.WriteLine($"Checkout for session: {session.Id} complited with status {session.Status}");
                    }
                    
                }
                else if (stripeEvent.Type == EventTypes.CheckoutSessionAsyncPaymentSucceeded)
                {
                    var session = stripeEvent.Data.Object as Session;

                    Console.WriteLine($"async checkout payment succeeded for session: {session.Id}");
                }
                else if (stripeEvent.Type == EventTypes.CheckoutSessionAsyncPaymentFailed)
                {
                    var session = stripeEvent.Data.Object as Session;

                    Console.WriteLine($"async checkout payment failed for session: {session.Id}");
                }
                else
                {
                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }
                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"error: {e.Message}");
                return BadRequest();
            }
        }

    }
}
