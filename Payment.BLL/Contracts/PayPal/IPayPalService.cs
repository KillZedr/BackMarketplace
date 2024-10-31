using Payment.Domain.ECommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayPalPayment = PayPal.Api.Payment;


namespace Payment.BLL.Contracts.PayPal
{
    public interface IPayPalService : IService
    {
        Task<PayPalPayment> CreatePayment(PaymentBasket basket);

        Task<bool> CancelPayment(string paymentId);

        Task<PayPalPayment> GetPaymentAsync(string paymentId);
    }
}
