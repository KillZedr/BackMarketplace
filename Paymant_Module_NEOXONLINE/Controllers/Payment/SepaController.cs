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
    [Route("api/[controller]")]
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


        [HttpPost("create-donation")]
        public async Task<IActionResult> CreateSepaDonation([FromBody] SepaDonationRequestDto request)
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
