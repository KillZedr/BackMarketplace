using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payment.Application.Payment_DAL.Contracts;
using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Microsoft.EntityFrameworkCore;
using Payment.BLL.Services.PayProduct;
using Payment.Domain;
using Payment.Domain.ECommerce;
using Payment.Domain.Identity;
using Payment.Domain.Stripe;
using Stripe;
using Stripe.Checkout;
using Stripe.Forwarding;
using Stripe.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Payment.Domain.DTOs;


namespace Payment.BLL.Services.Payment
{
    internal class StripeService : IStripeService
    {
        private readonly Stripe.ProductService _productService;
        private readonly PriceService _priceService;
        private readonly SessionService _sessionService;
        private readonly CustomerService _customerService;
        private readonly RefundService _refundService;
        private readonly PaymentIntentService _paymentIntentService;
        private readonly ILogger<StripeService> _logger;
        private readonly IUnitOfWork _unitOfWork;




        public StripeService(ILogger<StripeService> logger,  IUnitOfWork unitOfWork)
        {
            _productService = new Stripe.ProductService();
            _priceService = new PriceService();
            _sessionService = new SessionService();
            _customerService = new CustomerService();
            _refundService = new RefundService();
            _paymentIntentService = new PaymentIntentService();
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<StripeList<Product>> GetAllStripeProductsAsync()
        {
            return await _productService.ListAsync();
        }

        public async Task<Product> GetStripeProductAsync(string id)
        {
            return await _productService.GetAsync(id);
        }

        public async Task<string> CreateStripeProductAsync(ProductCreationDto productDto)
        {
            var productOptions = new ProductCreateOptions
            {
                Name = productDto.Name,
                Description = productDto.Description,
                Metadata = new Dictionary<string, string>
                {
                    { "CategoryName", productDto.CategoryName }
                },
                Shippable = false
            };
            var product = await _productService.CreateAsync(productOptions);

            return product.Id;
        }

        public async Task<string> CreateStripePriceAsync(string productId, decimal priceAmount)
        {
            var priceOptions = new PriceCreateOptions
            {
                UnitAmount = (long?)(priceAmount * 100),
                Currency = "eur",
                Product = productId,
            };
            var price = await _priceService.CreateAsync(priceOptions);

            return price.Id;
        }

        public async Task<Stripe.Product> UpdateStripeProductAsync(string id, ProductCreationDto productDto)
        {
            var productUpdateOptions = new ProductUpdateOptions 
            { 
                Name = productDto.Name,
                Description =  productDto.Description,
                Metadata = new Dictionary<string, string>
                {
                    { "CategoryName", productDto.CategoryName }
                }
            };
            var product = await _productService.UpdateAsync(id, productUpdateOptions);

            return product;
        }

        public async Task<bool> DeleteStripeProductAsync(string productId)
        {
            try
            {
                DeactivateProductPricesAsync(productId);
                var deletedProduct = await _productService.DeleteAsync(productId);//err
                return (bool)deletedProduct.Deleted;
            }
            catch (StripeException ex)
            {
                throw ex;
            }
        }

        private async Task DeactivateProductPricesAsync(string productId)
        {
            var prices = await _priceService.ListAsync(new PriceListOptions
            {
                Product = productId
            });
            foreach (var price in prices.Data)
            {
                if (price.Active)
                {
                    var updateOptions = new PriceUpdateOptions
                    {
                        Active = false
                    };
                    await _priceService.UpdateAsync(price.Id, updateOptions);
                }
            }
        }

        public async Task<bool> ArchiveStripeProductAsync(string id)
        {
            try
            {
                var updateOptions = new ProductUpdateOptions
                {
                    Active = false
                };

                var updatedProduct = await _productService.UpdateAsync(id, updateOptions);
                return !updatedProduct.Active;
            }
            catch (StripeException ex)
            {
                throw ex;
            }
        }

        public async Task<bool> ActivateStripeProductAsync(string id)
        {
            var updateOptions = new ProductUpdateOptions
            {
                Active = true
            };

            var updatedProduct = await _productService.UpdateAsync(id, updateOptions);
            return updatedProduct.Active;
        }

        public async Task<string> CreateCheckoutSessionAsync(List<string> prices, string customerId)
        {
            var lineItems = prices.Select(product => new SessionLineItemOptions
            {
                Price = product,
                Quantity = 1,
            }).ToList();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card"/*, "paypal", "sepa_debit"*/},
                LineItems = lineItems,
                Mode = "payment",
                Customer = customerId,
                //SuccessUrl = "https://your-website.com/success?session_id={CHECKOUT_SESSION_ID}",
                SuccessUrl = "https://www.meme-arsenal.com/memes/a2c78af09e451831566e7e90c4269a5c.jpg",
                CancelUrl = "https://cs14.pikabu.ru/images/previews_comm/2023-10_2/1696889858182579745.jpg",
            };
            Session session = await _sessionService.CreateAsync(options);            
            return session.Url;
        }

        public async Task<string?> GetStripePriceIdByProductIdAsync(string stripeProductId)
        {
            var price = (await _priceService.ListAsync(new PriceListOptions
            {
                Product = stripeProductId
            })).FirstOrDefault();
            if(price != null)
            {
                return price.Id;
            }
            return null;
        }

        public Customer CreateStripeCustomer(UserDto userDto)
        {            
            var customerOptions = new CustomerCreateOptions
            {
                Email = userDto.Email,
                Name = userDto.Name,
                Phone = userDto.PhoneNumber,
                Address = new AddressOptions
                {
                    Line1 = userDto.Address,
                    City = userDto.City,
                    Country = userDto.Country
                },                
            };
            var customer = _customerService.Create(customerOptions);

            return customer;
        }

        public async Task<string> CreateRefundAsync(string paymentIntentId, long amount, string reason)
        {
            try
            {
                var refundOptions = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Amount = amount, // Сумма возврата в центах (optional, для частичного возврата)
                    Reason = reason ?? "requested_by_customer" //"duplicate", "fraudulent", "requested_by_customer"
                };

                Refund refund = await _refundService.CreateAsync(refundOptions);
                return refund.Id;
            }
            catch (StripeException ex)
            {
                Console.WriteLine($"Error occurred during refund: {ex.Message} \n" +
                    $"Error code: {ex.StripeError?.Code} \n" +
                    $"Error type: {ex.StripeError?.Type}");

                if (ex.StripeError?.Code == "insufficient_funds")
                {
                    Console.WriteLine("Insufficient funds for refund.");
                    // Логика для обработки недостатка средств
                }
                else if (ex.StripeError?.Code == "invalid_request_error")
                {
                    Console.WriteLine("Invalid request. Please check payment ID or other parameters.");
                    // Логика для обработки ошибки с неверными параметрами запроса
                }
                else if (ex.StripeError?.Code == "api_error")
                {
                    Console.WriteLine("Stripe API error occured");
                    // Логика для обработки ошибки в Stripe API
                }
                else if (ex.StripeError?.Code == "card_error")
                {
                    Console.WriteLine("problem with the card occured (for example, expired)");
                    // Логика для обработки ошибки из-за проблем с картой (например, истек срок действия).
                }
                throw ex;
            }
        }

        public async Task<string> CreateDonationAsync(decimal amount, string currency, string customerId)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency,
                Customer = customerId,
                Metadata = new Dictionary<string, string>
                {
                    { "TransactionType", "Donation" }
                }
            };

            PaymentIntent intent = await _paymentIntentService.CreateAsync(options);

            return intent.ClientSecret;
        }

        public async Task<PaymentResultDto> ProcessGooglePayPaymentAsync(PaymentBasket basket, string googlePayToken)
        {
            var result = new PaymentResultDto();
            try
            {
                if (string.IsNullOrEmpty(googlePayToken))
                {
                    result.Success = false;
                    result.Message = "Google Pay token is missing.";
                    return result;
                }

                if (basket.Amount <= 0)
                {
                    result.Success = false;
                    result.Message = "Invalid payment amount.";
                    return result;
                }

                var paymentFee = await GetPaymentFeeByMethodAsync("card");

                if (paymentFee == null)
                {
                    result.Success = false;
                    result.Message = "Payment fee configuration for 'card' is missing.";
                    return result;
                }

                var totalAmount = basket.Amount + (basket.Amount * paymentFee.PercentageFee / 100) + paymentFee.FixedFee;

                var paymentMethodOptions = new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions
                    {
                        Token = googlePayToken
                    }
                };

                var paymentMethodService = new PaymentMethodService();
                var paymentMethod = await paymentMethodService.CreateAsync(paymentMethodOptions);

                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(totalAmount * 100),
                    Currency = "eur",
                    PaymentMethod = paymentMethod.Id,
                    Description = $"Google Pay payment for Basket ID: {basket.BasketId} on {DateTime.UtcNow}",
                    Metadata = new Dictionary<string, string>
            {
                { "TransactionType", "GooglePay" }
            },
                    Confirm = true,
                    ReturnUrl = "https://docs.stripe.com"
                };

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

                if (paymentIntent.Status == "succeeded")
                {
                    var chargeService = new ChargeService();
                    var charges = await chargeService.ListAsync(new ChargeListOptions
                    {
                        PaymentIntent = paymentIntent.Id
                    });

                    var charge = charges.Data.FirstOrDefault();
                    result.ReceiptUrl = charge?.ReceiptUrl;

                    result.Success = true;
                    result.Message = "Payment completed successfully.";
                    result.TransactionId = paymentIntent.Id;
                }
                else if (paymentIntent.Status == "processing")
                {
                    result.Success = false;
                    result.Message = "Payment is processing.";
                    result.TransactionId = paymentIntent.Id;
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Payment failed. Status: {paymentIntent.Status}";
                }
            }
            catch (StripeException ex)
            {
                result.Success = false;
                result.Message = $"Error processing payment: {ex.Message}";
            }

            return result;
        }

        public async Task<PaymentResultDto> ProcessSepaPaymentAsync(PaymentBasket basket, SepaPaymentRequestDto sepaRequest)
        {
            const int maxRetries = 5; // max try to connect
            const int retryDelay = 5000; //delay between trying 
            int attempt = 0;

            try
            {
                if (string.IsNullOrEmpty(sepaRequest.Iban))
                {
                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "IBAN is missing."
                    };
                }

                var paymentFee = await GetPaymentFeeByMethodAsync("sepa_debit");

                if (paymentFee == null)
                {
                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "Payment fee configuration for 'sepa_debit' is missing."
                    };
                }

                // Calculate the amount
                var totalAmount = basket.Amount + (basket.Amount * paymentFee.PercentageFee / 100) + paymentFee.FixedFee;

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(totalAmount * 100),
                    Currency = "eur",
                    PaymentMethodTypes = new List<string> { "sepa_debit" },
                    Metadata = new Dictionary<string, string>
            {
                { "TransactionType", "SEPAPay" }
            }
                };

                var paymentIntent = await _paymentIntentService.CreateAsync(options);

                var confirmOptions = new PaymentIntentConfirmOptions
                {
                    PaymentMethodData = new PaymentIntentPaymentMethodDataOptions
                    {
                        Type = "sepa_debit",
                        SepaDebit = new PaymentIntentPaymentMethodDataSepaDebitOptions
                        {
                            Iban = sepaRequest.Iban
                        },
                        BillingDetails = new PaymentIntentPaymentMethodDataBillingDetailsOptions
                        {
                            Email = basket.UserEmail,
                            Name = basket.Basket.User.FirstName
                        }
                    },
                    MandateData = new PaymentIntentMandateDataOptions
                    {
                        CustomerAcceptance = new PaymentIntentMandateDataCustomerAcceptanceOptions
                        {
                            Type = "online",
                            Online = new PaymentIntentMandateDataCustomerAcceptanceOnlineOptions
                            {
                                IpAddress = sepaRequest.IpAddress,
                                UserAgent = sepaRequest.UserAgent
                            }
                        }
                    }
                };

                var confirmedPaymentIntent = await _paymentIntentService.ConfirmAsync(paymentIntent.Id, confirmOptions);

                while (confirmedPaymentIntent.Status == "processing" && attempt < maxRetries)
                {
                    await Task.Delay(retryDelay); // Delay before retrying
                    attempt++;

                    // Повторно запрашиваем статус платежа
                    confirmedPaymentIntent = await _paymentIntentService.GetAsync(confirmedPaymentIntent.Id);
                }

                if (confirmedPaymentIntent.Status == "succeeded")
                {
                    string receiptUrl = null;
                    if (!string.IsNullOrEmpty(confirmedPaymentIntent.LatestChargeId))
                    {
                        var chargeService = new ChargeService();
                        var charge = await chargeService.GetAsync(confirmedPaymentIntent.LatestChargeId);
                        receiptUrl = charge?.ReceiptUrl;
                    }

                    return new PaymentResultDto
                    {
                        Success = true,
                        Message = "Payment completed successfully.",
                        TransactionId = confirmedPaymentIntent.Id,
                        ReceiptUrl = receiptUrl
                    };
                }

                if (confirmedPaymentIntent.Status == "processing")
                {
                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "Payment is still processing after maximum retries.",
                        TransactionId = confirmedPaymentIntent.Id
                    };
                }

                return new PaymentResultDto
                {
                    Success = false,
                    Message = $"Payment failed. Status: {confirmedPaymentIntent.Status}.",
                    TransactionId = confirmedPaymentIntent.Id
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Message = $"Stripe error: {ex.Message}."
                };
            }
            catch (Exception ex)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}."
                };
            }
        }

        public async Task<PaymentResultDto> CreateGooglePayDonationAsync(decimal amount, string currency, string googlePayToken, string customerId)
        {
            var result = new PaymentResultDto();
            try
            {
                if (amount <= 0)
                {
                    result.Success = false;
                    result.Message = "Donation amount must be greater than zero.";
                    return result;
                }

                if (string.IsNullOrEmpty(googlePayToken))
                {
                    result.Success = false;
                    result.Message = "Google Pay token is missing.";
                    return result;
                }

                var paymentFee = await GetPaymentFeeByMethodAsync("card");

                if (paymentFee == null)
                {
                    result.Success = false;
                    result.Message = "Payment fee configuration for 'card' is missing.";
                    return result;
                }

                // Рассчитать итоговую сумму
                var totalAmount = paymentFee != null
                    ? amount + (amount * paymentFee.PercentageFee / 100) + paymentFee.FixedFee
                    : amount;

                // Создание PaymentMethod
                var paymentMethodOptions = new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions
                    {
                        Token = googlePayToken
                    }
                };

                var paymentMethodService = new PaymentMethodService();
                var paymentMethod = await paymentMethodService.CreateAsync(paymentMethodOptions);

                // Создание PaymentIntent
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(totalAmount * 100),
                    Currency = currency,
                    PaymentMethod = paymentMethod.Id,
                    Customer = customerId,
                    Description = "Google Pay donation",
                    Metadata = new Dictionary<string, string>
            {
                { "DonationType", "GooglePayDonation" },
                { "TransactionType", "GooglePayDonation" }
            },
                    Confirm = true,
                    ReturnUrl = "https://docs.stripe.com"
                };

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

                // Проверка статуса платежа
                if (paymentIntent.Status == "succeeded")
                {
                    // Использование ChargeService для получения ReceiptUrl
                    var chargeService = new ChargeService();
                    var charges = await chargeService.ListAsync(new ChargeListOptions
                    {
                        PaymentIntent = paymentIntent.Id
                    });

                    var charge = charges.Data.FirstOrDefault();
                    result.ReceiptUrl = charge?.ReceiptUrl;

                    result.Success = true;
                    result.Message = "Donation completed successfully.";
                    result.TransactionId = paymentIntent.Id;
                }
                else if (paymentIntent.Status == "requires_action")
                {
                    result.Success = false;
                    result.Message = "Additional action required to complete donation.";
                    result.TransactionId = paymentIntent.Id;
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Donation failed. Status: {paymentIntent.Status}";
                }
            }
            catch (StripeException ex)
            {
                result.Success = false;
                result.Message = $"Error processing donation: {ex.Message}";
            }

            return result;
        }

        public async Task<PaymentResultDto> CreateSepaDonationAsync(SepaDonationRequestDto request, string customerId)
        {
            const int maxRetries = 5;  // max try to connect
            const int retryDelay = 5000; //delay between trying 
            int attempt = 0;

            try
            {
                var customerService = new CustomerService();
                var customer = await customerService.GetAsync(customerId);

                var paymentFee = await GetPaymentFeeByMethodAsync("sepa_debit");

                if (paymentFee == null)
                {
                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "Payment fee configuration for 'sepa_debit' is missing."
                    };
                }

                //total amount with fee
                var totalAmount = request.Amount + (request.Amount * paymentFee.PercentageFee / 100) + paymentFee.FixedFee;

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(totalAmount * 100),
                    Currency = request.Currency,
                    Customer = customerId,
                    PaymentMethodTypes = new List<string> { "sepa_debit" },
                    Metadata = new Dictionary<string, string>
            {
                { "TransactionType", "SEPA_Donation" }
            }
                };

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.CreateAsync(options);

                var confirmOptions = new PaymentIntentConfirmOptions
                {
                    PaymentMethodData = new PaymentIntentPaymentMethodDataOptions
                    {
                        Type = "sepa_debit",
                        SepaDebit = new PaymentIntentPaymentMethodDataSepaDebitOptions
                        {
                            Iban = request.SepaRequest.Iban
                        },
                        BillingDetails = new PaymentIntentPaymentMethodDataBillingDetailsOptions
                        {
                            Name = customer.Name,
                            Email = customer.Email
                        }
                    },
                    MandateData = new PaymentIntentMandateDataOptions
                    {
                        CustomerAcceptance = new PaymentIntentMandateDataCustomerAcceptanceOptions
                        {
                            Type = "online",
                            Online = new PaymentIntentMandateDataCustomerAcceptanceOnlineOptions
                            {
                                IpAddress = request.SepaRequest.IpAddress,
                                UserAgent = request.SepaRequest.UserAgent
                            }
                        }
                    }
                };

                var confirmedPaymentIntent = await paymentIntentService.ConfirmAsync(paymentIntent.Id, confirmOptions);

                //If the payment status is "processing", repeat the request until maxRetries
                while (confirmedPaymentIntent.Status == "processing" && attempt < maxRetries)
                {
                    await Task.Delay(retryDelay);
                    attempt++;
                    confirmedPaymentIntent = await paymentIntentService.GetAsync(confirmedPaymentIntent.Id);
                }

                if (confirmedPaymentIntent.Status == "succeeded")
                {
                    string receiptUrl = null;
                    if (!string.IsNullOrEmpty(confirmedPaymentIntent.LatestChargeId))
                    {
                        var chargeService = new ChargeService();
                        var charge = await chargeService.GetAsync(confirmedPaymentIntent.LatestChargeId);
                        receiptUrl = charge?.ReceiptUrl;
                    }

                    return new PaymentResultDto
                    {
                        Success = true,
                        Message = "Donation completed successfully.",
                        TransactionId = confirmedPaymentIntent.Id,
                        ReceiptUrl = receiptUrl
                    };
                }

                if (confirmedPaymentIntent.Status == "processing")
                {
                    return new PaymentResultDto
                    {
                        Success = false,
                        Message = "Donation is still processing after maximum retries.",
                        TransactionId = confirmedPaymentIntent.Id
                    };
                }

                return new PaymentResultDto
                {
                    Success = false,
                    Message = $"Donation failed. Status: {confirmedPaymentIntent.Status}.",
                    TransactionId = confirmedPaymentIntent.Id
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Message = $"Stripe error: {ex.Message}."
                };
            }
            catch (Exception ex)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}."
                };
            }
        }

        public PaymentFee ValidateAndPreparePaymentFee(PaymentFee fee)
        {
            // Проверка входных данных
            if (string.IsNullOrEmpty(fee.PaymentMethod))
            {
                throw new ArgumentException("Payment method is required.");
            }

            if (fee.PercentageFee < 0 || fee.FixedFee < 0)
            {
                throw new ArgumentException("Fees cannot be negative.");
            }

            if (string.IsNullOrEmpty(fee.Currency) || fee.Currency.Length != 3)
            {
                throw new ArgumentException("Invalid currency code.");
            }

            // Приводим валюту к верхнему регистру
            fee.Currency = fee.Currency.ToUpper();

            // Обновляем дату последнего изменения
            fee.LastUpdated = DateTime.UtcNow;

            return fee;
        }
        public async Task<PaymentFee?> GetPaymentFeeByMethodAsync(string paymentMethod)
        {
            if (string.IsNullOrEmpty(paymentMethod))
            {
                return null;
            }

            var repository = _unitOfWork.GetRepository<PaymentFee>();
            var paymentFee = await repository.AsQueryable()
                .FirstOrDefaultAsync(p => p.PaymentMethod.ToLower() == paymentMethod.ToLower());

            return paymentFee;

        }




    }
}
