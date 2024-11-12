using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.PayPal;
using Payment.BLL.Services.PayPal.EntityRefould;
using Payment.BLL.Settings.PayPalSetting;
using Payment.Domain.ECommerce;
using Payment.Domain.PayPal;
using PayPal;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly IPayPalCommissionService _commissionService;
        private readonly IUnitOfWork _unitOfWork;

        public PayPalService(IOptions<PayPalSettings> payPalSettings, ILogger<PayPalService> logger, IPayPalCommissionService commissionService, IUnitOfWork unitOfWork)
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
            _commissionService = commissionService;
            _unitOfWork = unitOfWork;
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

        public async Task<RefundResult> RefundPaymentAsync(string paymentId)
        {
            try
            {
                var findTransaction = _unitOfWork.GetRepository<PayPalPaymentTransaction>()
                    .AsQueryable()
                    .FirstOrDefault(ppt => ppt.PaymentId == paymentId);

                if (findTransaction == null)
                {
                    _logger.LogError($"Transaction with payment ID {paymentId} not found.");
                    return new RefundResult { IsSuccess = false };
                }

                // Получение транзакции по ID
                var sale = new Sale { id = findTransaction.SaleId };

                // Выполнение возврата
                var response = sale.Refund(_apiContext, new Refund
                {
                    amount = new Amount
                    {
                        total = findTransaction.Amount.ToString(),
                        currency = findTransaction.Currency
                    }
                });

                _logger.LogInformation($"Refund successful for payment ID: {paymentId}");

                // Возвращаем результат возврата
                return new RefundResult
                {
                    IsSuccess = true,
                    RefundTransactionId = response.id,
                    RefundAmount = response.amount.total,
                    Currency = response.amount.currency
                };
            }
            catch (PayPalException ex)
            {
                _logger.LogError(ex, $"PayPalException occurred while refunding payment ID: {paymentId}");
                return new RefundResult { IsSuccess = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception occurred while refunding payment ID: {paymentId}");
                return new RefundResult { IsSuccess = false };
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
                        cancel_url = "https://localhost:7257/api/paypal/cancel",
                        return_url = "https://localhost:7257/api/paypal/execute-payment"
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

        public async Task<string> CreateDonationPaymentAndGetApprovalUrlAsync(decimal price, string currency, string email)
        {
            var payment = new PayPalPayment
            {
                intent = "sale",
                payer = new Payer { payment_method = "paypal" },
                transactions = new List<Transaction>
        {
            new Transaction
            {
                amount = new Amount
                {
                    currency = currency,
                    total = price.ToString("F2", CultureInfo.InvariantCulture)
                },
                description = "Donation",
                custom = email // Custom data (email) for future reference
            }
        },
                redirect_urls = new RedirectUrls
                {
                    cancel_url = "https://localhost:7257/api/paypal/cancel",
                    return_url = "https://localhost:7257/api/paypal/execute-donation"
                }
            };

            // Создание платежа через PayPal API
            var createdPayment = payment.Create(_apiContext);
            var approvalUrl = createdPayment.links.FirstOrDefault(link => link.rel == "approval_url")?.href;

            return approvalUrl;
        }
        public async Task<string> CreatePaymentAndGetApprovalUrlAsync(PaymentBasket basket)
        {


            decimal commission = _commissionService.CalculateCommission(basket.Amount, "EUR");
            decimal totalAmount = basket.Amount + commission;

            
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
                        total = totalAmount.ToString("F2")
                    },
                    custom = basket.Id.ToString() // Передаем idPaymentBasket как custom data
                }
            },
                redirect_urls = new RedirectUrls
                {
                    cancel_url = "https://localhost:7257/api/paypal/cancel",
                    return_url = "https://localhost:7257/api/paypal/execute-payment"
                }
            };

            try
            {
                var createdPayment = payment.Create(_apiContext);
                var approvalUrl = createdPayment.links.FirstOrDefault(link => link.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase))?.href;
                return approvalUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment");
                return null;
            }
        }

        public async Task<PayPalPayment> ExecutePaymentAsync(string paymentId, string payerId)
        {
            var paymentExecution = new PaymentExecution() { payer_id = payerId };
            var payment = new PayPalPayment() { id = paymentId };

            try
            {
                var executedPayment = payment.Execute(_apiContext, paymentExecution);
                _logger.LogInformation($"Payment executed successfully: {executedPayment.id}");
                return executedPayment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing payment");
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
