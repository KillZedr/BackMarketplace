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
        /// <summary>
        /// Initializes a new instance of the <see cref="GooglePayController"/> class.
        /// </summary>
        /// <param name="stripeService">Service for processing payments through Stripe.</param>
        /// <param name="unitOfWork">Unit of work for data access.</param>
        /// <param name="logger">Logger for error handling and diagnostics.</param>
        public GooglePayController(IStripeService stripeService, IUnitOfWork unitOfWork, ILogger<GooglePayController> logger)
        {
            _stripeService = stripeService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Processes a Google Pay payment for a specific basket.
        /// </summary>
        /// <param name="basketId">The ID of the payment basket.</param>
        /// <param name="googlePayToken">The Google Pay token in JSON format.</param>
        /// <returns>
        /// A JSON response with:
        /// - <see cref="OkObjectResult"/>: If the payment was successfully completed.
        /// - <see cref="AcceptedResult"/>: If the payment is still processing.
        /// - <see cref="BadRequestObjectResult"/>: If input data is invalid or an error occurs during token parsing or payment processing.
        /// - <see cref="NotFoundObjectResult"/>: If the basket with the given ID is not found.
        /// </returns>
        [HttpPost("googlepay")]
        public async Task<IActionResult> ProcessGooglePayPayment([FromQuery] int basketId, [FromQuery] string googlePayToken)
        {
            if (basketId <= 0 || string.IsNullOrEmpty(googlePayToken))
            {
                return BadRequest("Invalid basket ID or Google Pay token.");
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

            // Попытка извлечь только `id` из переданного токена
            string tokenId;
            try
            {
                var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(googlePayToken);
                tokenId = tokenData?.id;
                if (string.IsNullOrEmpty(tokenId))
                {
                    return BadRequest(new { message = "Invalid token format." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error parsing token: {ex.Message}" });
            }

            // Выполняем оплату через сервис, используя `basket` и `tokenId`
            var result = await _stripeService.ProcessGooglePayPaymentAsync(basket, tokenId);

            if (result.Contains("completed successfully"))
            {
                return Ok(new { message = result });
            }
            else if (result.Contains("processing"))
            {
                return Accepted(new { message = result });
            }
            else
            {
                return BadRequest(new { message = result });
            }
        }


        /// <summary>
        /// Creates a Google Pay donation.
        /// </summary>
        /// <param name="amount">The donation amount.</param>
        /// <param name="currency">The currency of the donation (e.g., USD, EUR).</param>
        /// <param name="googlePayToken">The Google Pay token in JSON format.</param>
        /// <param name="customerId">The customer ID for the donation.</param>
        /// <returns>
        /// A JSON response with:
        /// - <see cref="OkObjectResult"/>: If the donation was successfully created.
        /// - <see cref="AcceptedResult"/>: If the donation is still processing.
        /// - <see cref="BadRequestObjectResult"/>: If input data is invalid or an error occurs during token parsing or donation creation.
        /// </returns>
        [HttpPost("create-donation")]
        public async Task<IActionResult> CreateGooglePayDonation([FromQuery] decimal amount, [FromQuery] string currency, [FromQuery] string googlePayToken, [FromQuery] string customerId)
        {
            if (amount <= 0 || string.IsNullOrEmpty(googlePayToken) || string.IsNullOrEmpty(currency) || string.IsNullOrEmpty(customerId))
            {
                return BadRequest("Invalid donation amount, currency, Google Pay token, or customer ID.");
            }

            // Попытка извлечь только `id` из переданного токена
            string tokenId;
            try
            {
                var tokenData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(googlePayToken);
                tokenId = tokenData?.id;
                if (string.IsNullOrEmpty(tokenId))
                {
                    return BadRequest("Invalid token format.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error parsing token: {ex.Message}");
            }

            // Передаем `amount`, `currency`, `tokenId`, и `customerId` в сервис
            var result = await _stripeService.CreateGooglePayDonationAsync(amount, currency, tokenId, customerId);

            if (result.Contains("completed successfully"))
            {
                return Ok(new { message = result });
            }
            else if (result.Contains("processing"))
            {
                return Accepted(new { message = result });
            }
            else
            {
                return BadRequest(new { message = result });
            }
        }

    }
}