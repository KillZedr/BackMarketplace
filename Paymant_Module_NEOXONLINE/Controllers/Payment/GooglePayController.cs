using Microsoft.AspNetCore.Mvc;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.Payment;
using Payment.Domain.ECommerce;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Paymant_Module_NEOXONLINE.Controllers.Payment
{
    [Route("billing/swagger/api/[controller]")]
    [ApiController]
    public class GooglePayController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GooglePayController> _logger;
        private readonly IConfiguration _configuration;

        public GooglePayController(
            IStripeService stripeService,
            IUnitOfWork unitOfWork,
            ILogger<GooglePayController> logger,
            IConfiguration configuration)
        {
            _stripeService = stripeService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Processes a Google Pay payment for a specific basket.
        /// </summary>
        /// <param name="basketId">
        /// The ID of the basket for which the payment is processed. Example: 123
        /// </param>
        /// <param name="googlePayToken">
        /// The Google Pay token used to process the payment. If not provided, a test token is retrieved from appsettings.json.
        /// Example:
        /// {
        ///     "id": "tok_1QOGMWBpHuilOt7EG4jlfNXJ",
        ///     "object": "token",
        ///     "card": {
        ///         "brand": "MasterCard",
        ///         "country": "US",
        ///         "exp_month": 10,
        ///         "exp_year": 2026,
        ///         "last4": "7897"
        ///     }
        /// }
        /// </param>
        /// <response code="200">
        /// Indicates a successful payment and returns the following details:
        /// {
        ///     "message": "Payment completed successfully.",
        ///     "transactionId": "pi_1QOGMWBpHuilOt7EG4jlfNXJ",
        ///     "receiptUrl": "https://pay.stripe.com/receipts/..."
        /// }
        /// </response>
        /// <response code="202">
        /// Indicates that the payment is still being processed:
        /// {
        ///     "message": "Payment is processing.",
        ///     "transactionId": "pi_1QOGMWBpHuilOt7EG4jlfNXJ",
        ///     "receiptUrl": null
        /// }
        /// </response>
        /// <response code="400">
        /// Indicates an invalid request due to missing or incorrect data:
        /// {
        ///     "message": "Invalid basket ID."
        /// }
        /// </response>
        /// <response code="404">
        /// Indicates that the specified basket could not be found:
        /// {
        ///     "message": "Basket with ID 123 not found."
        /// }
        /// </response>
        /// <response code="500">
        /// Indicates an unexpected server error occurred during payment processing.
        /// Example:
        /// {
        ///     "message": "An unexpected error occurred during payment processing."
        /// }
        /// </response>
        /// <remarks>
        /// This method processes a Google Pay payment for a specified basket. If the `googlePayToken` is not provided,
        /// a test token is retrieved from the configuration. The total amount is calculated with applicable fees, and
        /// the payment is processed. On success, payment details are returned, including a receipt URL.
        /// </remarks>
        [HttpPost("googlepay")]
        public async Task<IActionResult> ProcessGooglePayPayment([FromQuery] int basketId, [FromQuery] string googlePayToken = null)
        {
            if (basketId <= 0)
            {
                return BadRequest("Invalid basket ID.");
            }

            // Если токен не передан, используем тестовый токен из appsettings.json
            if (string.IsNullOrEmpty(googlePayToken))
            {
                googlePayToken = _configuration["GooglePay:TestToken"];
                if (string.IsNullOrEmpty(googlePayToken))
                {
                    return BadRequest("Google Pay token is required and was not provided.");
                }
            }
            // Попытка извлечь только `id` из переданного токена
            string tokenId;
            try
            {
                var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(googlePayToken);
                tokenId = tokenData?.id;

                if (string.IsNullOrEmpty(tokenId))
                {
                    return BadRequest(new { message = "Invalid token format. 'id' is missing." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error parsing token: {ex.Message}" });
            }


            // Загружаем корзину из базы данных по `basketId`
            var basket = _unitOfWork.GetRepository<PaymentBasket>()
                .AsQueryable()
                .Include(pb => pb.Basket)
                .ThenInclude(b => b.User)
                .FirstOrDefault(pb => pb.Id == basketId);

            if (basket == null)
            {
                return NotFound(new { message = $"Basket with ID {basketId} not found." });
            }

            // Выполняем оплату через сервис, используя `basket` и `googlePayToken`
            var result = await _stripeService.ProcessGooglePayPaymentAsync(basket, tokenId);

            if (result.Success)
            {
                return Ok(new
                {
                    message = result.Message,
                    transactionId = result.TransactionId,
                    receiptUrl = result.ReceiptUrl
                });
            }
            else if (result.Message.Contains("processing"))
            {
                return Accepted(new
                {
                    message = result.Message,
                    transactionId = result.TransactionId,
                    receiptUrl = result.ReceiptUrl
                });
            }
            else
            {
                return BadRequest(new { message = result.Message });
            }
        }

        /// <summary>
        /// Processes a donation using Google Pay for a specified amount and customer.
        /// </summary>
        /// <param name="amount">The amount of the donation to be processed.</param>
        /// <param name="currency">The currency in which the donation is made (e.g., USD, EUR).</param>
        /// <param name="googlePayToken">
        /// The Google Pay token to process the donation. If not provided, a test token from appsettings.json is used.
        /// </param>
        /// <param name="customerId">The ID of the stripe customer associated with the donation. Example: cus_RDOMbImfVQlG1b</param>
        /// <response code="200">
        /// Returns donation success details including message, transaction ID, and receipt URL.
        /// </response>
        /// <response code="202">
        /// Indicates that the donation is being processed. Returns transaction ID and receipt URL.
        /// </response>
        /// <response code="400">
        /// Indicates an invalid request. Examples: invalid donation amount, missing currency, or customer ID.
        /// </response>
        /// <response code="500">
        /// Indicates an unexpected server error occurred during donation processing.
        /// </response>
        /// <remarks>
        /// This method processes a donation using Google Pay. If a Google Pay token is not provided,
        /// a test token is retrieved from appsettings.json. It calculates the total amount using the configured
        /// payment fee and creates a payment intent for the donation. On success, it returns donation details,
        /// including a receipt URL.
        /// </remarks>
        [HttpPost("create-donation")]
        public async Task<IActionResult> CreateGooglePayDonation([FromQuery] decimal amount, [FromQuery] string currency, [FromQuery] string googlePayToken = null, [FromQuery] string customerId = null)
        {
            if (amount <= 0 || string.IsNullOrEmpty(currency) || string.IsNullOrEmpty(customerId))
            {
                return BadRequest("Invalid donation amount, currency, or customer ID.");
            }

            // Если токен не передан, используем тестовый токен из appsettings.json
            if (string.IsNullOrEmpty(googlePayToken))
            {
                googlePayToken = _configuration["GooglePay:TestToken"];
                if (string.IsNullOrEmpty(googlePayToken))
                {
                    return BadRequest("Google Pay token is required and was not provided.");
                }
            }
            // Попытка извлечь только `id` из переданного токена
            string tokenId;
            try
            {
                var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(googlePayToken);
                tokenId = tokenData?.id;

                if (string.IsNullOrEmpty(tokenId))
                {
                    return BadRequest(new { message = "Invalid token format. 'id' is missing." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error parsing token: {ex.Message}" });
            }


            // Передаем `amount`, `currency`, `googlePayToken`, и `customerId` в сервис
            var result = await _stripeService.CreateGooglePayDonationAsync(amount, currency, tokenId, customerId);

            if (result.Success)
            {
                return Ok(new
                {
                    message = result.Message,
                    transactionId = result.TransactionId,
                    receiptUrl = result.ReceiptUrl
                });
            }
            else if (result.Message.Contains("processing"))
            {
                return Accepted(new
                {
                    message = result.Message,
                    transactionId = result.TransactionId,
                    receiptUrl = result.ReceiptUrl
                });
            }
            else
            {
                return BadRequest(new { message = result.Message });
            }
        }
    }
}