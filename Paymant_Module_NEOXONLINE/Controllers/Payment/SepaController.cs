﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Payment.Domain.ECommerce;
using Payment.Application.Payment_DAL.Contracts;
using System.Threading.Tasks;

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
        public async Task<IActionResult> ProcessSepaPayment([FromBody] SepaPaymentRequest sepaRequest, [FromQuery] int basketId)
        {
            var basket = _unitOfWork.GetRepository<PaymentBasket>()
                .AsQueryable()
                .FirstOrDefault(pb => pb.Id == basketId);

            if (basket == null)
            {
                return NotFound(new { message = $"Basket with ID {basketId} not found." });
            }

            if (basket.Amount <= 0 || basket.Basket.User == null)
            {
                return BadRequest(new { message = "Invalid basket data or missing user information." });
            }

            var resultMessage = await _stripeService.ProcessSepaPaymentAsync(basket, sepaRequest);

            if (resultMessage.Contains("Payment completed successfully"))
            {
                return Ok(new { success = true, message = resultMessage });
            }
            else if (resultMessage.Contains("processing"))
            {
                return Accepted(new { success = true, message = resultMessage });
            }
            else
            {
                _logger.LogError("Failed to process SEPA payment for basket ID: {BasketId}", basketId);
                return BadRequest(new { success = false, message = resultMessage });
            }
        }
        [HttpPost("create-donation")]
        public async Task<IActionResult> CreateSepaDonation([FromBody] SepaDonationRequest request)
        {
            if (request.Amount <= 0 || string.IsNullOrEmpty(request.SepaRequest?.Iban))
            {
                return BadRequest(new { message = "Invalid donation amount or missing IBAN." });
            }

            var result = await _stripeService.CreateSepaDonationAsync(
                request.Amount,
                request.Currency,
                request.SepaRequest,
                request.User
            );

            if (result.Contains("Donation completed successfully"))
            {
                return Ok(new { success = true, message = result });
            }
            else if (result.Contains("processing"))
            {
                return Accepted(new { success = true, message = result });
            }
            else
            {
                _logger.LogError("Failed to process SEPA donation.");
                return BadRequest(new { success = false, message = result });
            }
        }
    }
}
