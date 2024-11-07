using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.Notifications;
using Payment.BLL.Contracts.PayPal;
using Payment.Domain.ECommerce;
using Payment.Domain.PayPal;
using PayPal.Api;
using System.Globalization;

namespace Paymant_Module_NEOXONLINE.Controllers.PayPalC
{
    [Route("api/[controller]")]
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



        [HttpGet("all")]
        public async Task<IActionResult> GetAllTransactions ()
        {
            var repoTransactions = await _unitOfWork.GetRepository<PayPalPaymentTransaction>().AsReadOnlyQueryable().ToListAsync();
            return Ok(repoTransactions);
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
                await _emailNotificationService.SendSuccessNotificationAsync(userEmail, idPaymentBasket.ToString());

                return Ok(new { approvalUrl });
            }

            return BadRequest(new { message = "Failed to create payment" });
        }
    


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

                var transaction = new PayPalPaymentTransaction
                {
                    
                    PaymentId = result.id,
                    PayerId = PayerID,
                    SaleId = result.transactions[0].related_resources[0].sale.id,
                    Status = result.state,
                    Amount = decimal.Parse(result.transactions[0].amount.total, CultureInfo.InvariantCulture),
                    Currency = result.transactions[0].amount.currency,
                    Description = "Payment description"
                };

                var repoPayPalTransaction = _unitOfWork.GetRepository<PayPalPaymentTransaction>();
                repoPayPalTransaction.Create(transaction);
                await _unitOfWork.SaveShangesAsync();
                
                /*await _emailNotificationService.SendSuccessNotificationAsync(userEmail, paymentId);*/

                return Ok(new { message = "Payment executed successfully", paymentId = result.id });
            }
            else
            {
                _logger.LogError("Error executing payment");
                return StatusCode(500, "Error executing payment");
            }
        }


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

                /*var refundTransaction = new PayPalPaymentTransaction
                {
                    PaymentId = paymentId,
                    Status = "Refunded",
                    RefundId = refundResult.RefundTransactionId,
                    Amount = refundResult.RefundAmount,
                    Currency = refundResult.Currency,
                    Description = "Refund processed",
                    RefundedDate = DateTime.UtcNow
                };*/

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

