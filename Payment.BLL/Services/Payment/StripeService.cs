﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Payment.BLL.Contracts.Payment;
using Payment.BLL.DTOs;
using Payment.BLL.Services.PayProduct;
using Payment.Domain;
using Payment.Domain.ECommerce;
using Payment.Domain.Identity;
using Stripe;
using Stripe.Checkout;
using Stripe.Forwarding;
using Stripe.Terminal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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


        public StripeService(ILogger<StripeService> logger)
        {
            _productService = new Stripe.ProductService();
            _priceService = new PriceService();
            _sessionService = new SessionService();
            _customerService = new CustomerService();
            _refundService = new RefundService();
            _paymentIntentService = new PaymentIntentService();
            _logger = logger;
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

        public async Task<string> ProcessGooglePayPaymentAsync(PaymentBasket basket, string googlePayToken)
        {
            try
            {
                if (string.IsNullOrEmpty(googlePayToken))
                {
                    return "Google Pay token is missing.";
                }

                if (basket.Amount <= 0)
                {
                    return "Invalid payment amount.";
                }

                // Create a PaymentMethod using the Google Pay token
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

                //Create a PaymentIntent using the created PaymentMethod
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(basket.Amount * 100), 
                    Currency = "eur",
                    PaymentMethod = paymentMethod.Id, 
                    Description = $"Google Pay payment for Basket ID: {basket.BasketId} on {DateTime.UtcNow}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "TransactionType", "GooglePay" }
                    },
                    Confirm = true, // Automatically confirm the payment
                    ReturnUrl = "https://docs.stripe.com", 
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    }
                };

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

                // Check the status of the PaymentIntent and return the appropriate message
                if (paymentIntent.Status == "succeeded")
                {
                    _logger.LogInformation("Google Pay payment succeeded for Basket ID: {BasketId}", basket.BasketId);
                    return $"Payment completed successfully. Transaction ID: {paymentIntent.Id}";
                }
                else if (paymentIntent.Status == "pending" || paymentIntent.Status == "processing")
                {
                    _logger.LogInformation("Google Pay payment is processing for Basket ID: {BasketId}", basket.BasketId);
                    return $"Payment is processing. Transaction ID: {paymentIntent.Id}, Status: {paymentIntent.Status}";
                }
                else
                {
                    _logger.LogWarning("Google Pay payment failed for Basket ID: {BasketId}, Status: {Status}", basket.BasketId, paymentIntent.Status);
                    return $"Payment failed. Status: {paymentIntent.Status}";
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error processing Google Pay payment for Basket ID: {BasketId}", basket.BasketId);
                return $"Error processing payment: {ex.Message}";
            }
        }

        public async Task<string> ProcessSepaPaymentAsync(PaymentBasket basket, SepaPaymentRequest sepaRequest)
        {
            try
            {
                if (string.IsNullOrEmpty(sepaRequest.Iban))
                {
                    throw new ArgumentException("IBAN is missing.");
                }

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(basket.Amount * 100),
                    Currency = "eur",
                    PaymentMethodTypes = new List<string> { "sepa_debit" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "TransactionType", "SEPAPay" }
                    },

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

                if (confirmedPaymentIntent.Status == "succeeded")
                {
                    _logger.LogInformation("SEPA payment succeeded for Basket ID: {BasketId}", basket.BasketId);
                    return $"Payment completed successfully. Transaction ID: {confirmedPaymentIntent.Id}";
                }
                else if (confirmedPaymentIntent.Status == "processing")
                {
                    _logger.LogInformation("SEPA payment is processing for Basket ID: {BasketId}", basket.BasketId);
                    return $"Payment is processing. Transaction ID: {confirmedPaymentIntent.Id}";
                }
                else
                {
                    _logger.LogWarning("SEPA payment failed for Basket ID: {BasketId}, Status: {Status}", basket.BasketId, confirmedPaymentIntent.Status);
                    return "Payment failed.";
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error processing SEPA payment for Basket ID: {BasketId}", basket.BasketId);
                return $"Error processing payment: {ex.Message}";
            }
        }

        public async Task<string> CreateGooglePayDonationAsync(decimal amount, string currency, string googlePayToken, string customerId)
        {
            try
            {
                if (amount <= 0)
                {
                    return "Donation amount must be greater than zero.";
                }

                if (string.IsNullOrEmpty(googlePayToken))
                {
                    return "Google Pay token is missing.";
                }

                //  Create a PaymentMethod using the Google Pay token
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

                // Create a PaymentIntent using the created PaymentMethod
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), 
                    Currency = currency,
                    PaymentMethod = paymentMethod.Id,
                    Customer = customerId,
                    Description = "Google Pay donation",
                    Metadata = new Dictionary<string, string>
                    {
                        { "DonationType", "GooglePayDonation" },
                        { "TransactionType", "GooglePayDonation" }
                    },
                    Confirm = true, // Automatically confirm
                    ReturnUrl = "https://docs.stripe.com",  
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };

                var paymentIntentService = new PaymentIntentService();
                var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

                // Check payment status and return result
                if (paymentIntent.Status == "succeeded")
                {
                    _logger.LogInformation("Google Pay donation succeeded. Transaction ID: {TransactionId}", paymentIntent.Id);
                    return $"Donation completed successfully. Transaction ID: {paymentIntent.Id}";
                }
                else if (paymentIntent.Status == "requires_action")
                {
                    return $"Additional action required to complete donation. PaymentIntent ID: {paymentIntent.Id}";
                }
                else
                {
                    return $"Donation failed. PaymentIntent ID: {paymentIntent.Id}, Status: {paymentIntent.Status}";
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error processing Google Pay donation.");
                return $"Error processing donation: {ex.Message}";
            }
        }
        
        public async Task<string> CreateSepaDonationAsync(SepaDonationRequest request, string customerId)
        {
            try
            {
                var customerService = new CustomerService();
                var customer = await customerService.GetAsync(customerId);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100),
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

                if (confirmedPaymentIntent.Status == "succeeded")
                {
                    _logger.LogInformation("SEPA donation succeeded. Transaction ID: {TransactionId}", confirmedPaymentIntent.Id);
                    return $"Donation completed successfully. Transaction ID: {confirmedPaymentIntent.Id}";
                }
                else if (confirmedPaymentIntent.Status == "processing")
                {
                    _logger.LogInformation("SEPA donation is processing. Transaction ID: {TransactionId}", confirmedPaymentIntent.Id);
                    return $"Donation is processing. Transaction ID: {confirmedPaymentIntent.Id}";
                }
                else
                {
                    _logger.LogWarning("SEPA donation failed. Status: {Status}", confirmedPaymentIntent.Status);
                    return "Donation failed.";
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error processing SEPA donation.");
                return $"Error processing donation: {ex.Message}";
            }
        }

    }
}
