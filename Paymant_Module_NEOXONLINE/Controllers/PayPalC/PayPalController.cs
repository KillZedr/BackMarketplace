﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.PayPal;
using Payment.Domain.ECommerce;

namespace Paymant_Module_NEOXONLINE.Controllers.PayPalC
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayPalController : ControllerBase
    {
        private readonly IPayPalService _payPalService;
        private readonly ILogger<PayPalController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public PayPalController(IPayPalService payPalService, ILogger<PayPalController> logger, IUnitOfWork unitOfWork)
        {
            _payPalService = payPalService;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }


        [HttpPost("Create")]
        public async Task<IActionResult> CreatePaymentAsync([FromForm] int findPaymentBasket)
        {
            var findPayment = _unitOfWork.GetRepository<PaymentBasket>()
                .AsQueryable()
                .FirstOrDefault(pb => pb.Id == findPaymentBasket);
            if (findPayment == null)
            {
                return NotFound(new { message = $"Not Found Payment Basket with {findPaymentBasket} id" });
            }
            else
            {
                if (findPayment == null || findPayment.Amount <= 0)
                {
                    return BadRequest(new { massege = "Invalid payment basket data" });
                }
                var payment = await _payPalService.CreatePaymentAsync(findPayment);
                if (payment == null)
                {
                    _logger.LogError($"Failed to create payment for basket with ID: {findPayment.BasketId}", findPayment.BasketId);
                    return StatusCode(500, "Failed to create payment");
                }
                return Ok(payment);
            }
        }


        [HttpPost("Cancel")]
        public async Task<IActionResult> CancelPayment([FromQuery] string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return BadRequest(new { massege = "Payment Id is required" });
            }

            var result = await _payPalService.CancelPaymentAsync(paymentId);

            if (!result)
            {
                _logger.LogError($"Failed to cancel payment with ID : {paymentId}", paymentId);

                return StatusCode(500, "Filed to cancel payment");
            }
            return Ok("Payment cancelled successfully");
        }


        [HttpGet("{paymentId}")]
        public async Task<IActionResult> GetPayment([FromRoute] string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return BadRequest("Payment ID is required.");
            }

            var payment = await _payPalService.GetPaymentAsync(paymentId);
            if (payment == null)
            {
                _logger.LogError("Payment not found with ID: {PaymentId}", paymentId);
                return NotFound("Payment not found.");
            }

            return Ok(payment);
        }
    }
}

