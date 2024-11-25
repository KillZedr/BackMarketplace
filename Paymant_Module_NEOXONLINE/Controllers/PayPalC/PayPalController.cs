using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.Notifications;
using Payment.BLL.Contracts.PayPal;
using Payment.BLL.CurrencyDonationAndPay;
using Payment.Domain.ECommerce;
using Payment.Domain.PayPal;
using PayPal.Api;
using System.Globalization;

namespace Paymant_Module_NEOXONLINE.Controllers.PayPalC
{
    [Route("billing/swagger/api/[controller]")]
    [ApiController]
    public class PayPalController : ControllerBase
    {
        private readonly IPayPalService _payPalService;
        private readonly ILogger<PayPalController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailNotificationService _emailNotificationService;
        public PayPalController(IPayPalService payPalService, ILogger<PayPalController> logger, IUnitOfWork unitOfWork, IEmailNotificationService emailNotificationService)
        {
            _payPalService = payPalService;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _emailNotificationService = emailNotificationService;
        }


        /// <summary>
        /// Retrieves all PayPal payment transactions from the database.
        /// </summary>
        /// <returns>
        /// A JSON array of all transactions:
        /// <list type="bullet">
        /// <item><description><c>200 OK</c>: List of transactions.</description></item>
        /// </list>
        /// </returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllTransactions()
        {
            var repoTransactions = await _unitOfWork.GetRepository<PayPalPaymentTransaction>().AsReadOnlyQueryable().ToListAsync();
            return Ok(repoTransactions);
        }
        /// <summary>
        /// Creates a payment using a payment basket.
        /// </summary>
        /// <param name="findPaymentBasket">
        /// The ID of the payment basket to create the payment for.
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
        /// <description>Payment created successfully. Returns payment details.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>Invalid payment basket data.</description>
        /// </item>
        /// <item>
        /// <term><c>404 Not Found</c></term>
        /// <description>Payment basket not found.</description>
        /// </item>
        /// <item>
        /// <term><c>500 Internal Server Error</c></term>
        /// <description>Failed to create payment.</description>
        /// </item>
        /// </list>
        /// </returns>
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

        /// <summary>
        /// Creates a PayPal donation payment and returns the approval URL.
        /// </summary>
        /// <param name="email">The email of the donor.</param>
        /// <param name="price">The donation amount.</param>
        /// <param name="currency">The currency for the donation (e.g., USD).</param>
        /// <returns>
        /// A JSON response with the following possible outcomes:
        /// <list type="table">
        /// <listheader>
        /// <term>Status Code</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term><c>200 OK</c></term>
        /// <description>Donation approval URL generated successfully.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>Invalid donation details provided.</description>
        /// </item>
        /// </list>
        /// </returns>
        [HttpPost("create-donation")]
        public async Task<IActionResult> CreateDonation([FromQuery] string email, [FromQuery] decimal price, [FromQuery] CurrencyDto currency)
        {

            // Используем currency.ToString(), чтобы передать строковое значение валюты в платежный сервис
            string currencyCode = currency.ToString();
            if (string.IsNullOrEmpty(email) || price <= 0 || string.IsNullOrEmpty(currencyCode))
            {
                return BadRequest("Invalid donation details provided.");
            }

            // Создаем данные для custom поля (для использования при подтверждении платежа)
            var customData = new { Email = email };
            var customJson = JsonConvert.SerializeObject(customData);

            // Генерация платежа через PayPal
            var approvalUrl = await _payPalService.CreateDonationPaymentAndGetApprovalUrlAsync(price, currencyCode, customJson);

            if (!string.IsNullOrEmpty(approvalUrl))
            {
                return Ok(new { approvalUrl });
            }

            return BadRequest(new { message = "Failed to create donation payment" });
        }
        /// <summary>
        /// Executes a PayPal donation payment.
        /// </summary>
        /// <param name="paymentId">The PayPal payment ID.</param>
        /// <param name="PayerID">The Payer ID returned by PayPal.</param>
        /// <returns>
        /// A JSON response with the following possible outcomes:
        /// <list type="table">
        /// <listheader>
        /// <term>Status Code</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term><c>200 OK</c></term>
        /// <description>Donation executed successfully. Returns transaction details.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>Invalid paymentId or PayerID.</description>
        /// </item>
        /// <item>
        /// <term><c>500 Internal Server Error</c></term>
        /// <description>Error executing donation.</description>
        /// </item>
        /// </list>
        /// </returns>
        [HttpGet("execute-donation")]
        public async Task<IActionResult> ExecuteDonation(string paymentId, string PayerID)
        {
            if (string.IsNullOrEmpty(paymentId) || string.IsNullOrEmpty(PayerID))
            {
                _logger.LogError("Invalid paymentId or PayerID");
                return BadRequest("Invalid paymentId or PayerID");
            }

            var result = await _payPalService.ExecutePaymentAsync(paymentId, PayerID);

            if (result != null && result.state == "approved")
            {
                _logger.LogInformation($"Donation executed successfully: {result.id}");

                // Извлечение email из custom поля
                var customData = JsonConvert.DeserializeObject<dynamic>(result.transactions[0].custom);
                string email = customData?.Email;

                // Отправка уведомления на email о подтверждении оплаты
                await _emailNotificationService.SendDonationSuccessNotificationAsync(email, result.transactions[0].amount.total,
                    result.transactions[0].amount.currency, DateTime.UtcNow);


                var transaction = new PayPalPaymentTransaction
                {
                    PaymentId = result.id,
                    PayerId = PayerID,
                    SaleId = result.transactions[0].related_resources[0].sale.id,
                    Status = result.state,
                    Amount = decimal.Parse(result.transactions[0].amount.total, CultureInfo.InvariantCulture),
                    Currency = result.transactions[0].amount.currency,
                    Description = $"Donation approved {result.payer.payer_info.email}"

                };


                var repoPayPalTransactions = _unitOfWork.GetRepository<PayPalPaymentTransaction>();

                repoPayPalTransactions.Create(transaction);
                await _unitOfWork.SaveShangesAsync();

                return Ok(new { message = "Donation executed successfully", paymentId = result.id });
            }
            else
            {
                _logger.LogError("Error executing donation");
                return StatusCode(500, "Error executing donation");
            }
        }
        /// <summary>
        /// Creates a PayPal approval URL for a payment basket.
        /// </summary>
        /// <param name="idPaymentBasket">The ID of the payment basket.</param>
        /// <param name="userEmail">The email of the user.</param>
        /// <returns>
        /// A JSON response with the following possible outcomes:
        /// <list type="table">
        /// <listheader>
        /// <term>Status Code</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term><c>200 OK</c></term>
        /// <description>Approval URL created successfully.</description>
        /// </item>
        /// <item>
        /// <term><c>404 Not Found</c></term>
        /// <description>Payment basket not found.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>Failed to create payment.</description>
        /// </item>
        /// </list>
        /// </returns>
        [HttpPost("createApprovalUrl")]
        public async Task<IActionResult> CreateApprovalUrl([FromQuery] int idPaymentBasket, [FromQuery] string userEmail)
        {
            // Проверяем, существует ли корзина с указанным id
            var findPaymentBasket = await _unitOfWork.GetRepository<PaymentBasket>()
                .AsQueryable()
                .FirstOrDefaultAsync(pb => pb.Id == idPaymentBasket);

            if (findPaymentBasket == null)
            {
                return NotFound(new { message = "Payment basket not found" });
            }

            // Сохраняем email пользователя в корзине
            findPaymentBasket.UserEmail = userEmail;
            _unitOfWork.GetRepository<PaymentBasket>().Update(findPaymentBasket);
            await _unitOfWork.SaveShangesAsync();

            // Генерируем approvalUrl
            var approvalUrl = await _payPalService.CreatePaymentAndGetApprovalUrlAsync(findPaymentBasket);

            if (!string.IsNullOrEmpty(approvalUrl))
            {
                // Отправляем уведомление о необходимости подтверждения платежа
                await _emailNotificationService.SendTestSuccessNotificationAsync(userEmail, DateTime.UtcNow, approvalUrl);

                return Ok(new { approvalUrl });
            }

            return BadRequest(new { message = "Failed to create payment" });
        }


        /// <summary>
        /// Executes a PayPal payment.
        /// </summary>
        /// <param name="paymentId">The PayPal payment ID.</param>
        /// <param name="PayerID">The Payer ID returned by PayPal.</param>
        /// <returns>
        /// A JSON response with the following possible outcomes:
        /// <list type="table">
        /// <listheader>
        /// <term>Status Code</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term><c>200 OK</c></term>
        /// <description>Payment executed successfully. Returns transaction details.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>Invalid paymentId or PayerID.</description>
        /// </item>
        /// <item>
        /// <term><c>500 Internal Server Error</c></term>
        /// <description>Error executing payment.</description>
        /// </item>
        /// </list>
        /// </returns>
        [HttpGet("execute-payment")]
        public async Task<IActionResult> ExecutePayment(string paymentId, string PayerID)
        {
            if (string.IsNullOrEmpty(paymentId) || string.IsNullOrEmpty(PayerID))
            {
                _logger.LogError("Invalid paymentId or PayerID");
                return BadRequest("Invalid paymentId or PayerID");
            }

            var result = await _payPalService.ExecutePaymentAsync(paymentId, PayerID);


            if (result != null && result.state == "approved")
            {
                _logger.LogInformation($"Payment executed successfully: {result.id}");

                int idPaymentBasket = int.Parse(result.transactions[0].custom);

                var transaction = new PayPalPaymentTransaction
                {

                    PaymentId = result.id,
                    PayerId = PayerID,
                    SaleId = result.transactions[0].related_resources[0].sale.id,
                    Status = result.state,
                    Amount = decimal.Parse(result.transactions[0].amount.total, CultureInfo.InvariantCulture),
                    Currency = result.transactions[0].amount.currency,
                    Description = $"Payment approved {result.payer.payer_info.email}",
                    PaymentBasketId = idPaymentBasket
                };

                var repoPayPalTransaction = _unitOfWork.GetRepository<PayPalPaymentTransaction>();
                repoPayPalTransaction.Create(transaction);
                await _unitOfWork.SaveShangesAsync();

                var repoPaymentBasket = await _unitOfWork.GetRepository<PaymentBasket>()
                   .AsQueryable()
                   .FirstOrDefaultAsync(pb => pb.Id == transaction.PaymentBasketId);

                await _emailNotificationService.SendSuccessNotificationAsync(repoPaymentBasket.UserEmail, transaction.Amount, transaction.Currency, transaction.CreatedDate);

                return Ok(new { message = "Payment executed successfully", paymentId = result.id });
            }
            else
            {
                _logger.LogError("Error executing payment");
                return StatusCode(500, "Error executing payment");
            }
        }

        /// <summary>
        /// Processes a refund for a PayPal payment.
        /// </summary>
        /// <param name="paymentId">The PayPal payment ID to refund.</param>
        /// <returns>
        /// A JSON response with the following possible outcomes:
        /// <list type="table">
        /// <listheader>
        /// <term>Status Code</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term><c>200 OK</c></term>
        /// <description>Payment refunded successfully. Returns refund transaction ID.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>Payment ID is required.</description>
        /// </item>
        /// <item>
        /// <term><c>500 Internal Server Error</c></term>
        /// <description>Error processing refund.</description>
        /// </item>
        /// </list>
        /// </returns>
        [HttpPost("Refund")]
        public async Task<IActionResult> RefundPayment([FromQuery] string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                return BadRequest(new { message = "Payment ID is required" });
            }

            var findTransaction = await _unitOfWork.GetRepository<PayPalPaymentTransaction>()
                .AsQueryable()
                .FirstOrDefaultAsync(ppt => ppt.PaymentId == paymentId);

            try
            {
                // Выполняем возврат средств через сервис
                var refundResult = await _payPalService.RefundPaymentAsync(paymentId);

                if (!refundResult.IsSuccess)
                {
                    _logger.LogError($"Failed to refund payment with ID: {paymentId}", paymentId);
                    return StatusCode(500, new { message = "Failed to refund payment" });
                }

                // Сохраняем информацию о возврате в базе данных

                findTransaction.Status = "Refunded";
                findTransaction.RefundedDate = DateTime.UtcNow;
                findTransaction.Description = "Refund processed";
                findTransaction.RefundId = refundResult.RefundTransactionId;

                var repoPayPalTransaction = _unitOfWork.GetRepository<PayPalPaymentTransaction>();
                repoPayPalTransaction.Update(findTransaction);
                await _unitOfWork.SaveShangesAsync();

                return Ok(new { message = "Payment refunded successfully", refundTransactionId = refundResult.RefundTransactionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while processing refund for payment ID: {paymentId}");
                return StatusCode(500, new { message = "An error occurred while processing refund" });
            }
        }




        /// <summary>
        /// Cancels a PayPal payment.
        /// </summary>
        /// <param name="paymentId">The PayPal payment ID to cancel.</param>
        /// <returns>
        /// A JSON response with the following possible outcomes:
        /// <list type="table">
        /// <listheader>
        /// <term>Status Code</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term><c>200 OK</c></term>
        /// <description>Payment cancelled successfully.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>Payment ID is required.</description>
        /// </item>
        /// <item>
        /// <term><c>500 Internal Server Error</c></term>
        /// <description>Error cancelling payment.</description>
        /// </item>
        /// </list>
        /// </returns>

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

        /// <summary>
        /// Retrieves a PayPal payment by its ID.
        /// </summary>
        /// <param name="paymentId">The ID of the PayPal payment.</param>
        /// <returns>
        /// A JSON response with the following possible outcomes:
        /// <list type="table">
        /// <listheader>
        /// <term>Status Code</term>
        /// <description>Description</description>
        /// </listheader>
        /// <item>
        /// <term><c>200 OK</c></term>
        /// <description>Payment retrieved successfully.</description>
        /// </item>
        /// <item>
        /// <term><c>404 Not Found</c></term>
        /// <description>Payment not found.</description>
        /// </item>
        /// <item>
        /// <term><c>400 Bad Request</c></term>
        /// <description>Payment ID is required.</description>
        /// </item>
        /// </list>
        /// </returns>
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

