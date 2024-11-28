using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Payment.Domain.ECommerce;
using Payment.Application.Payment_DAL.Contracts;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Paymant_Module_NEOXONLINE.Controllers
{
    [Route("billing/swagger/api/[controller]")]
    [ApiController]
    public class SepaController : ControllerBase
    {
        private readonly IStripeService _stripeService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SepaController> _logger;

        public SepaController(IStripeService stripeService, IUnitOfWork unitOfWork, ILogger<SepaController> logger)
        {
            _stripeService = stripeService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        /// <summary>
        /// Processes a SEPA payment for the specified basket.
        /// </summary>
        /// <param name="basketId">
        /// The ID of the basket for which the payment is processed. Example: 123
        /// </param>
        /// <param name="sepaRequest">
        /// The SEPA payment request containing IBAN, IP address, and user agent.
        /// </param>
        /// <response code="200">
        /// Payment success details:
        /// {
        ///     "success": true,
        ///     "message": "Payment completed successfully.",
        ///     "transactionId": "pi_1QOGMWBpHuilOt7EG4jlfNXJ",
        ///     "receiptUrl": "https://pay.stripe.com/receipts/..."
        /// }
        /// </response>
        /// <response code="202">
        /// Payment is being processed and may take some time to complete:
        /// {
        ///     "success": true,
        ///     "message": "Payment is still processing.",
        ///     "transactionId": "pi_1QOGMWBpHuilOt7EG4jlfNXJ",
        ///     "receiptUrl": null
        /// }
        /// </response>
        /// <response code="400">
        /// Invalid request due to incorrect data or missing information:
        /// {
        ///     "message": "Invalid basket data or missing user information."
        /// }
        /// </response>
        /// <response code="404">
        /// The specified basket was not found:
        /// {
        ///     "message": "Basket with ID 123 not found."
        /// }
        /// </response>
        /// <response code="500">
        /// Unexpected server error during payment processing:
        /// {
        ///     "message": "An unexpected error occurred while processing the payment."
        /// }
        /// </response>
        /// <remarks>
        /// <b>Example Request:</b>
        /// <br/>
        /// POST /api/Sepa/sepa
        /// <br/>
        /// Content-Type: multipart/form-data
        /// <br/>
        /// {
        ///     "basketId": 123,
        ///     "iban": "DE89370400440532013000",
        ///     "ipAddress": "192.168.1.1",
        ///     "userAgent": "Mozilla/5.0"
        /// }
        /// </remarks>
        [HttpPost("sepa")]
        public async Task<IActionResult> ProcessSepaPayment([FromForm] SepaPaymentRequestDto sepaRequest, [FromQuery] int basketId)
        {

            var basket = _unitOfWork.GetRepository<PaymentBasket>()
                .AsQueryable()
                .Include(pb => pb.Basket)
                .ThenInclude(b => b.User)
                .FirstOrDefault(pb => pb.Id == basketId);
            if (basket == null)
            {
                return NotFound(new { message = $"Basket with ID {basketId} not found." });
            }

            if (basket.Amount <= 0 || basket.Basket.User == null)
            {
                return BadRequest(new { message = "Invalid basket data or missing user information." });
            }

            // Обрабатываем платеж
            var paymentResult = await _stripeService.ProcessSepaPaymentAsync(basket, sepaRequest);

            if (paymentResult.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = paymentResult.Message,
                    transactionId = paymentResult.TransactionId,
                    receiptUrl = paymentResult.ReceiptUrl
                });
            }
            else if (paymentResult.Message.Contains("processing", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(new
                {
                    success = true,
                    message = paymentResult.Message,
                    transactionId = paymentResult.TransactionId,
                    receiptUrl = paymentResult.ReceiptUrl
                });
            }
            else
            {
                _logger.LogError("Failed to process SEPA payment for basket ID: {BasketId}", basketId);
                return BadRequest(new { success = false, message = paymentResult.Message });
            }
        }

        /// <summary>
        /// Processes a SEPA donation for the specified customer and payment details.
        /// </summary>
        /// <param name="request">
        /// The SEPA donation request containing the following fields:
        /// <list type="bullet">
        /// <item><term>Amount</term> - The donation amount. Example: 50.75</item>
        /// <item><term>Currency</term> - The currency of the donation. Example: "EUR"</item>
        /// <item><term>SepaRequest</term> - Contains SEPA payment details:
        /// <list type="bullet">
        /// <item><term>Iban</term> - IBAN of the payer. Example: "DE89370400440532013000"</item>
        /// <item><term>IpAddress</term> - Client IP address. Example: "192.168.1.1"</item>
        /// <item><term>UserAgent</term> - User agent string of the client. Example: "Mozilla/5.0"</item>
        /// </list>
        /// </item>
        /// <item><term>CustomerId</term> - The ID of the Stripe customer. Example: "cus_12345"</item>
        /// </list>
        /// </param>
        /// <response code="200">
        /// Donation success details:
        /// {
        ///     "success": true,
        ///     "message": "Donation completed successfully.",
        ///     "transactionId": "pi_1QOGMWBpHuilOt7EG4jlfNXJ",
        ///     "receiptUrl": "https://pay.stripe.com/receipts/..."
        /// }
        /// </response>
        /// <response code="202">
        /// Donation is being processed:
        /// {
        ///     "success": false,
        ///     "message": "Donation is still processing.",
        ///     "transactionId": "pi_1QOGMWBpHuilOt7EG4jlfNXJ"
        /// }
        /// </response>
        /// <response code="400">
        /// Invalid request due to missing or invalid data:
        /// {
        ///     "message": "Invalid donation amount, missing IBAN, or customer ID."
        /// }
        /// </response>
        /// <response code="500">
        /// Unexpected server error during donation processing:
        /// {
        ///     "message": "An unexpected error occurred while processing the donation."
        /// }
        /// </response>
        /// <remarks>
        /// <b>Example Request:</b>
        /// <br/>
        /// POST /api/Sepa/create-donation
        /// <br/>
        /// Content-Type: application/json
        /// <br/>
        /// {
        ///     "amount": 50.75,
        ///     "currency": "EUR",
        ///     "sepaRequest": {
        ///         "iban": "DE89370400440532013000",
        ///         "ipAddress": "192.168.1.1",
        ///         "userAgent": "Mozilla/5.0"
        ///     },
        ///     "customerId": "cus_RDOMbImfVQlG1b"
        /// }
        /// </remarks>
        [HttpPost("create-donation")]
        public async Task<IActionResult> CreateSepaDonation([FromForm] SepaDonationRequestDto request)
        {
            if (request.Amount <= 0 || string.IsNullOrEmpty(request.SepaRequest?.Iban) || string.IsNullOrEmpty(request.CustomerId))
            {
                return BadRequest(new { message = "Invalid donation amount, missing IBAN, or customer ID." });
            }

            var result = await _stripeService.CreateSepaDonationAsync(request, request.CustomerId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    transactionId = result.TransactionId,
                    receiptUrl = result.ReceiptUrl
                });
            }
            else if (result.Message.Contains("processing", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(new
                {
                    success = false,
                    message = result.Message,
                    transactionId = result.TransactionId
                });
            }
            else
            {
                _logger.LogError("Failed to process SEPA donation: {Message}", result.Message);
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    transactionId = result.TransactionId
                });
            }
        }
    }
}
