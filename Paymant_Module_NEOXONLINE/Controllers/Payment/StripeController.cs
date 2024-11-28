using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Payment.Domain.DTOs;
using Payment.Domain.ECommerce;
using Payment.Domain.PayProduct;
using Payment.Domain.Stripe;
using Stripe;
using Stripe.Checkout;
using Stripe.FinancialConnections;
using Stripe.Terminal;
using Stripe.V2;


namespace Paymant_Module_NEOXONLINE.Controllers.Payment
{
    [Route("billing/swagger/api/[controller]")]
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
        /// <response code="200">Returns all stripe products</response>
		/// <response code="404">No product was found</response>
        /// <response code="500">Server error</response>
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
        /// <param name="id">Product id in stripe</param>
        /// <response code="200">Returns stripe product</response>
        /// <response code="404">Product with such id not found</response>
        /// <response code="500">Server error</response>
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
        /// <response code="200">Returns the stripe product id and the stripe price id associated with the created product</response>
        /// <response code="500">Server error</response>
        [HttpPost("CreateProduct")]
        public async Task<IActionResult> CreateProduct(ProductCreationDto productDto)
        {
            try
            {
                var productId = await _stripeService.CreateStripeProductAsync(productDto);
                var priceId = await _stripeService.CreateStripePriceAsync(productId, (decimal)productDto.Price);
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
        /// <param name="productIds">List of product id in stripe</param>
        /// <param name="customerId">Customer id in stripe</param>
        /// <response code="200">Returns the payment link</response>
        /// <response code="404">One or more products have no prices</response>
        /// <response code="500">Server error</response>
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
        /// <response code="200">Returns stripe customer</response>
        /// <response code="500">Server error</response>
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
        /// <param name="id">Product id in stripe</param>
        /// <response code="200">Returns updated product</response>
        /// <response code="500">Server error</response>
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
        /// <param name="id">Product id in stripe</param>
        /// <response code="200">Returns updated product</response>
        /// <response code="500">Server error</response>
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
        /// <remarks>Archive product to disable so that it can’t be added to new invoices or subscriptions. any existing subscriptions that use the product remain active until they’re canceled and any existing payment links that use the product are deactivated. You can’t delete products that have an associated price, but you can archive them.</remarks>
        /// <param name="id">Product id in stripe</param>
        /// <response code="200">Returns true if product archived successfully</response>
        /// <response code="500">Server error</response>
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
        /// <param name="productId">Product id in stripe</param>
        /// <response code="200">Returns true if product deleted successfully</response>
        /// <response code="500">Server error</response>
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
        /// Creates a refund request
        /// </summary> 
        /// <param name="paymentIntentId">Payment intent id in stripe</param>
        /// <param name="amount">Amount of money to refund</param>
        /// <param name="reason">Reason of refund</param>
        /// <response code="200">Returns refund id</response>
        /// <response code="500">Server error</response>
        [HttpPost("CreateRefund")]
        public async Task<IActionResult> CreateRefund(string paymentIntentId, decimal amount, string reason)
        {
            try
            {
                return Ok(await _stripeService.CreateRefundAsync(paymentIntentId, (long)amount, reason));
            }
            catch (StripeException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Processes the donation
        /// </summary> 
        /// <param name="amount">Amount of money to donate</param>
        /// <param name="currency">Currency of money</param>
        /// <param name="customerId">Customer id in stripe</param>
        /// <response code="200">Returns clientSecret that should be processed on the front</response>
        /// <response code="400">Incorrect amount of donation</response>
        /// <response code="500">Server error</response>
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
        /// Handles stripe requests
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
                    else if (transactionType == "GooglePay")
                    {
                        var transaction = new StripeTransaction
                        {
                            StripeSessionId = paymentIntent.Id,
                            PaymentIntentId = paymentIntent.Id,
                            Amount = (long)(paymentIntent.Amount / 100m),
                            Currency = paymentIntent.Currency,
                            PaymentMethod = "Google Pay",
                            CreatedAt = DateTime.UtcNow,
                            PaymentStatus = "succeeded"
                        };
                        _unitOfWork.GetRepository<StripeTransaction>().Create(transaction);
                        await _unitOfWork.SaveShangesAsync();

                        Console.WriteLine("Google Pay payment completed.");
                    }
                    else if (transactionType == "GooglePayDonation")
                    {
                        var transaction = new StripeDonation
                        {

                            PaymentIntentId = paymentIntent.Id,
                            Amount = paymentIntent.Amount / 100m,
                            CustomerId = paymentIntent.CustomerId,
                            PaymentMethod = "Google Pay",
                            Currency = paymentIntent.Currency,
                            CreatedAt = DateTime.UtcNow,
                            IsSuccessful = true
                        };
                        _unitOfWork.GetRepository<StripeDonation>().Create(transaction);
                        await _unitOfWork.SaveShangesAsync();

                        Console.WriteLine("Google Pay donation completed.");
                    }
                    else if (transactionType == "SEPAPay")
                    {
                        var transaction = new StripeTransaction
                        {
                            StripeSessionId = stripeEvent.Id,
                            PaymentIntentId = paymentIntent.Id,
                            Amount = (long)(paymentIntent.Amount / 100m),
                            Currency = paymentIntent.Currency,
                            PaymentMethod = "SEPA Debit",
                            PaymentStatus = paymentIntent.Status,
                            CreatedAt = DateTime.UtcNow,
                            CustomerId = paymentIntent.CustomerId,
                            ClientIp = paymentIntent.Metadata.ContainsKey("IpAddress") ? paymentIntent.Metadata["IpAddress"] : null
                        };
                        _unitOfWork.GetRepository<StripeTransaction>().Create(transaction);
                        await _unitOfWork.SaveShangesAsync();

                        Console.WriteLine("SEPA Pay payment completed.");
                    }
                    else if (transactionType == "SEPA_Donation")
                    {
                        var transaction = new StripeDonation
                        {

                            PaymentIntentId = paymentIntent.Id,
                            Amount = paymentIntent.Amount / 100m,
                            CustomerId = paymentIntent.CustomerId,
                            PaymentMethod = "SEPA Debit",
                            Currency = paymentIntent.Currency,
                            CreatedAt = DateTime.UtcNow,
                            IsSuccessful = true
                        };
                        _unitOfWork.GetRepository<StripeDonation>().Create(transaction);
                        await _unitOfWork.SaveShangesAsync();

                        Console.WriteLine("SEPA Pay donation completed.");
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
        /// <summary>
        /// Adds or updates a payment fee for a specific payment method and currency.
        /// </summary>
        /// <param name="feeDto">
        /// The payment fee data transfer object containing details of the fee:
        /// <list type="bullet">
        /// <item><description><c>PaymentMethod</c>: The payment method (e.g., Credit Card, PayPal).</description></item>
        /// <item><description><c>PercentageFee</c>: The percentage fee to be applied (e.g., 2.5 for 2.5%).</description></item>
        /// <item><description><c>FixedFee</c>: The fixed fee amount to be added (e.g., 1.50 for 1.50 in the given currency).</description></item>
        /// <item><description><c>Currency</c>: The currency for which the fee is applied (e.g., USD, EUR).</description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// A JSON response with the following possible outcomes:
        /// <list type="table">
        /// <listheader>
        /// <term>Status Code</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term><c>200 OK</c></term>
        /// <description>Fee added or updated successfully. Contains a success message.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>The input data is null or invalid. Contains an error message.</description>
        /// </item>
        /// <item>
        /// <term><c>409 Conflict</c></term>
        /// <description>A payment fee with the same method and currency already exists. Contains an error message.</description>
        /// </item>
        /// <item>
        /// <term><c>500 Internal Server Error</c></term>
        /// <description>An unexpected error occurred. Includes error details and inner exception messages.</description>
        /// </item>
        /// </list>
        /// </returns>
        [HttpPost("AddOrUpdatePaymentFee")]
        public async Task<IActionResult> AddOrUpdatePaymentFee([FromBody] PaymentFeeDto feeDto)
        {
            try
            {
                if (feeDto == null)
                {
                    return BadRequest(new { Error = "Payment fee data is required." });
                }

                // Создаём объект PaymentFee на основе DTO
                var fee = new PaymentFee
                {
                    PaymentMethod = feeDto.PaymentMethod,
                    PercentageFee = feeDto.PercentageFee,
                    FixedFee = feeDto.FixedFee,
                    Currency = feeDto.Currency.ToUpper(),
                    LastUpdated = DateTime.UtcNow
                };

                // Валидируем данные
                fee = _stripeService.ValidateAndPreparePaymentFee(fee);

                // Получаем репозиторий
                var repository = _unitOfWork.GetRepository<PaymentFee>();

                // Проверяем, существует ли уже такая запись
                var existingFee = await repository.AsQueryable()
                    .FirstOrDefaultAsync(f => f.PaymentMethod.ToLower() == fee.PaymentMethod.ToLower() &&
                                              f.Currency.ToLower() == fee.Currency.ToLower());

                if (existingFee != null)
                {
                    // Обновляем существующую запись
                    existingFee.PercentageFee = fee.PercentageFee;
                    existingFee.FixedFee = fee.FixedFee;

                    // Обновляем LastUpdated для существующей записи
                    existingFee.LastUpdated = DateTime.UtcNow;

                    repository.Update(existingFee);
                    await _unitOfWork.SaveShangesAsync();

                    return Ok(new { Message = $"Payment fee for method '{fee.PaymentMethod}' in currency '{fee.Currency}' updated successfully." });
                }

                // Добавляем новую запись
                repository.Create(fee);
                await _unitOfWork.SaveShangesAsync();

                return Ok(new { Message = $"Payment fee for method '{fee.PaymentMethod}' in currency '{fee.Currency}' added successfully." });
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("IX_PaymentMethod_Currency_Unique") == true)
            {
                return Conflict(new { Error = "A payment fee with this method and currency already exists." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "An unexpected error occurred.",
                    Details = ex.Message,
                    InnerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Retrieves a list of all payment fees.
        /// </summary>
        /// <response code="200">
        /// Returns the list of all payment fees. Example:
        /// [
        ///     {
        ///         "PaymentMethod": "card",
        ///         "PercentageFee": 2.5,
        ///         "FixedFee": 1.0,
        ///         "Currency": "USD"
        ///     },
        ///     {
        ///         "PaymentMethod": "sepa_debit",
        ///         "PercentageFee": 1.0,
        ///         "FixedFee": 0.5,
        ///         "Currency": "EUR"
        ///     }
        /// ]
        /// </response>
        /// <response code="404">
        /// Returns when no payment fees are found.
        /// Example:
        /// {
        ///     "message": "No payment fees found."
        /// }
        /// </response>
        /// <response code="500">
        /// Returns when an unexpected server error occurs.
        /// Example:
        /// {
        ///     "error": "An unexpected error occurred.",
        ///     "details": "Detailed error message."
        /// }
        /// </response>
        /// <remarks>
        /// This endpoint retrieves all payment fees stored in the system.
        /// </remarks>
        [HttpGet("GetAllPaymentFees")]
        public async Task<IActionResult> GetAllPaymentFees()
        {
            try
            {
                // Получаем репозиторий PaymentFee
                var repository = _unitOfWork.GetRepository<PaymentFee>();

                // Получаем все записи
                var paymentFees = await repository.AsQueryable().ToListAsync();

                if (paymentFees == null || !paymentFees.Any())
                {
                    return NotFound(new { message = "No payment fees found." });
                }

                // Преобразуем в DTO
                var paymentFeeDtos = paymentFees.Select(fee => new PaymentFeeDto
                {
                    PaymentMethod = fee.PaymentMethod,
                    PercentageFee = fee.PercentageFee,
                    FixedFee = fee.FixedFee,
                    Currency = fee.Currency
                });

                return Ok(paymentFeeDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An unexpected error occurred.",
                    details = ex.Message
                });
            }
        }

    }
}
