using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Payment.Domain;
using Payment.Domain.ECommerce;
using Payment.Domain.PayProduct;
using Stripe;
using Stripe.Checkout;
using Stripe.FinancialConnections;
using Stripe.Terminal;
using Stripe.V2;
using static Paymant_Module_NEOXONLINE.Controllers.CurrancyLayer.CurrencyLayerController;

namespace Paymant_Module_NEOXONLINE.Controllers.Payment
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly string _webhookSecret;
        private readonly IUnitOfWork _unitOfWork;

        public StripeController(IStripeService stripeService, IConfiguration configuration, IUnitOfWork unitOfWork)
        {
            _stripeService = stripeService;
            _webhookSecret = configuration["Stripe:WebhookSecret"];
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets stripe products
        /// </summary> 
        /// <response code="200">returns all stripe products</response>
		/// <response code="404">no product was found</response>
        /// <response code="500">server error</response>
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

        /// <summary>
        /// Gets stripe product by stripe product id
        /// </summary> 
        /// <param name="id">product id in stripe</param>
        /// <response code="200">returns stripe product</response>
        /// <response code="404">product with such id not found</response>
        /// <response code="500">server error</response>
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

        /// <summary>
        /// Creates stripe product
        /// </summary> 
        /// <response code="200">returns the stripe product id and the stripe price id associated with the created product</response>
        /// <response code="500">server error</response>
        [HttpPost("CreateProduct")]
        public async Task<IActionResult> CreateProduct(ProductCreationDto productDto)
        {
            try
            {
                var productId = await _stripeService.CreateStripeProductAsync(productDto);
                var priceId = await _stripeService.CreateStripePriceAsync(productId, productDto.Price);
                return Ok(new { ProductId = productId, PriceId = priceId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Creates checkout session
        /// </summary> 
        /// <param name="productIds">list of product id in stripe</param>
        /// <param name="customerId">customer id in stripe</param>
        /// <response code="200">returns the payment link</response>
        /// <response code="404">one or more products have no prices</response>
        /// <response code="500">server error</response>
        [HttpPost("CreateCheckoutSession")]
        public async Task<IActionResult> CreateCheckoutSession(List<string> productIds, string customerId)
        {
            try
            {
                //todo
                //check if a product exists
                //check if an user exists
                List<string> prices = new List<string>();
                foreach (var productId in productIds)
                {
                    var price = await _stripeService.GetStripePriceIdByProductIdAsync(productId);
                    if (price != null)
                    {
                        prices.Add(price);
                    }
                    else
                    {
                        return NotFound($"price for product {productId} not found");
                    }
                }
                return Ok(await _stripeService.CreateCheckoutSessionAsync(prices, customerId));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Creates stripe customer
        /// </summary> 
        /// <response code="200">returns stripe customer</response>
        /// <response code="500">server error</response>
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

        /// <summary>
        /// Updates stripe product
        /// </summary> 
        /// <param name="id">product id in stripe</param>
        /// <response code="200">returns updated product</response>
        /// <response code="500">server error</response>
        [HttpPut("UpdateProduct")]
        public async Task<IActionResult> UpdateProduct(string id, ProductCreationDto productDto)
        {
            try
            {
                //todo
                //check if product id exists
                return Ok(await _stripeService.UpdateStripeProductAsync(id, productDto));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Updates stripe product
        /// </summary> 
        /// <param name="id">product id in stripe</param>
        /// <response code="200">returns updated product</response>
        /// <response code="500">server error</response>
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

        /// <summary>
        /// Archives stripe product
        /// </summary> 
        /// <remarks>archive product to disable so that it can’t be added to new invoices or subscriptions. any existing subscriptions that use the product remain active until they’re canceled and any existing payment links that use the product are deactivated. You can’t delete products that have an associated price, but you can archive them.</remarks>
        /// <param name="id">product id in stripe</param>
        /// <response code="200">returns true if product archived successfully</response>
        /// <response code="500">server error</response>
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

        /// <summary>
        /// Deletes stripe product
        /// </summary> 
        /// <remarks>You can only delete products that have no prices associated with them. </remarks>
        /// <param name="productId">product id in stripe</param>
        /// <response code="200">returns true if product deleted successfully</response>
        /// <response code="500">server error</response>
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

        /// <summary>
        /// creates a refund request
        /// </summary> 
        /// <param name="paymentIntentId">payment intent id in stripe</param>
        /// <param name="amount">amount of money to refund</param>
        /// <param name="reason">reason of refund</param>
        /// <response code="200">returns refund id</response>
        /// <response code="500">server error</response>
        [HttpPost("CreateRefund")]
        public async Task<IActionResult> CreateRefund(string paymentIntentId, decimal amount, string reason)
        {
            try
            {
                return Ok(await _stripeService.CreateRefundAsync(paymentIntentId, amount, reason));
            }
            catch (StripeException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// processes the donation
        /// </summary> 
        /// <param name="amount">amount of money to donate</param>
        /// <param name="currency">currency of money</param>
        /// <param name="customerId">customer id in stripe</param>
        /// <response code="200">returns clientSecret that should be processed on the front</response>
        /// <response code="400">incorrect amount of donation</response>
        /// <response code="500">server error</response>
        [HttpPost("donate")]
        public async Task<IActionResult> Donate(decimal amount, string currency, string customerId)
        {
            if (amount <= 0)
            {
                return BadRequest("Donation amount must be greater than zero.");
            }

            var secret = _stripeService.CreateDonationAsync(amount, currency, customerId);

            return Ok(new { clientSecret = secret });
        }

        /// <summary>
        /// handles stripe requests
        /// </summary> 
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
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    var transaction = new StripeTransaction
                    {
                        StripeSessionId = session?.Id,
                        PaymentIntentId = session?.PaymentIntentId,
                        PaymentMethod = session?.PaymentMethodTypes[0],
                        PaymentStatus = session?.PaymentStatus ?? "unknown",
                        CreatedAt = DateTime.UtcNow,
                        Currency = session?.Currency,
                        Amount = session?.AmountTotal ?? 0,
                        CustomerId = session?.CustomerId,
                        ClientIp = HttpContext.Connection.RemoteIpAddress.ToString() //???
                    };

                    if (session?.PaymentStatus == "paid")
                    {
                        Console.WriteLine($"Checkout completed for session: {session.Id}");
                    }
                    else
                    {
                        Console.WriteLine($"Checkout for session: {session.Id} complited with status {session.PaymentStatus}");
                    }

                    _unitOfWork.GetRepository<StripeTransaction>().Create(transaction);
                    await _unitOfWork.SaveShangesAsync();
                }
                else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    var transaction = new StripeTransaction
                    {
                        PaymentIntentId = paymentIntent?.Id,
                        PaymentStatus = "failed",
                        Amount = paymentIntent?.Amount ?? 0,
                        Currency = paymentIntent?.Currency,
                        CustomerId = paymentIntent?.CustomerId,
                        StatusReason = paymentIntent?.LastPaymentError?.Message,
                        CreatedAt = DateTime.UtcNow
                    };

                    _unitOfWork.GetRepository<StripeTransaction>().Create(transaction);
                    await _unitOfWork.SaveShangesAsync();

                    Console.WriteLine($"Payment failed for PaymentIntent: {paymentIntent?.Id}");
                }
                else if (stripeEvent.Type == EventTypes.ChargeRefunded)
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    Console.WriteLine($"Charge refunded: {charge.Id}");
                    //todo add refund info to db 
                }
                else if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    if (paymentIntent.Metadata.TryGetValue("TransactionType", out var transactionType) && transactionType == "Donation")
                    {
                        var donation = new StripeDonation
                        {
                            PaymentIntentId = paymentIntent.Id,
                            Amount = paymentIntent.Amount / 100m,
                            CustomerId = paymentIntent.CustomerId,
                            Currency = paymentIntent.Currency,
                            CreatedAt = DateTime.UtcNow,
                            IsSuccessful = true
                        };
                        _unitOfWork.GetRepository<StripeDonation>().Create(donation);
                        await _unitOfWork.SaveShangesAsync();

                        Console.WriteLine("Donation completed.");
                    }
                    else
                    {
                        // Логика для других типов транзакций
                        Console.WriteLine("Non-donation payment completed.");
                    }
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
