using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.BLL.Contracts.PayPal;
using Payment.BLL.PayPalSetting;
using Payment.Domain.ECommerce;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayPalPayment = PayPal.Api.Payment;

namespace Payment.BLL.Services.PayPal
{
    public class PayPalService : IPayPalService
    {
        private readonly PayPalSettings _payPalSetting;

        private readonly APIContext _apiContext;

        private readonly ILogger<PayPalService> _logger;

        public PayPalService(IOptions<PayPalSettings> payPalSettings, ILogger<PayPalService> logger)
        {
            _payPalSetting = payPalSettings.Value;
            _logger = logger; // Инициализация логгера Microsoft.Extensions.Logging;
            _apiContext = new APIContext(new OAuthTokenCredential(
                _payPalSetting.ClientId,
                _payPalSetting.ClientSecret
            ).GetAccessToken())
            {
                Config = new Dictionary<string, string> { { "mode", _payPalSetting.Mode } }
            };
        }


        public async Task<bool> CancelPaymentAsync(string paymentId)
        {
            try
            {
                var payment = PayPalPayment.Get(_apiContext, paymentId);

                if (payment != null && payment.state == "created")
                {

                    //записывать в бд
                    return await Task.FromResult(true);
                }
                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling payment");
                return false;
            }
        }


        public async Task<PayPalPayment> CreatePaymentAsync(PaymentBasket basket)
        {
            try
            {
                var payment = new PayPalPayment
                {
                    intent = "sale",
                    payer = new Payer { payment_method = "paypal" },
                    transactions = new List<Transaction>
                    {
                        new Transaction
                        {
                            description = basket.MetaData,
                            amount = new Amount
                            {
                                currency = "EUR",
                                total = basket.Amount.ToString()
                            }
                        }
                    },
                    redirect_urls = new RedirectUrls
                    {
                        cancel_url = "https://www.sandbox.paypal.com/cgi-bin/webscr?cmd=_express-checkout&token=EC-07J31612JN5610411", //  заменить на свой 
                        return_url = "https://localhost:7257"  // заменить на свой
                    }
                };
                var createdPayment = payment.Create(_apiContext);
                return await Task.FromResult(createdPayment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return null;
            }
        }
        public async Task<string> CreatePaymentAndGetApprovalUrlAsync(PaymentBasket basket)
        {
            var payment = await CreatePaymentAsync(basket);  // Создаем платеж
            if (payment != null)
            {
                var approvalUrl = payment.links.FirstOrDefault(link => link.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;
                return approvalUrl;  // Возвращаем URL для перенаправления пользователя
            }
            return null;  // В случае ошибки возвращаем null
        }

        public async Task<PayPalPayment> ExecutePaymentAsync(string paymentId, string payerId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            var payment = new PayPalPayment() { id = paymentId };

            try
            {
                var executedPayment =  payment.Execute(_apiContext, paymentExecution);
                _logger.LogInformation($"Payment executed successfully: {executedPayment.id}");
                return executedPayment;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing payment: {ex.Message}");
                return null;
            }
        }


        public async Task<PayPalPayment> GetPaymentAsync(string paymentId)
            {
                try
                {
                    return await Task.FromResult(PayPalPayment.Get(_apiContext, paymentId));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving payment");
                    return null;
                }
            }
        }
    }
