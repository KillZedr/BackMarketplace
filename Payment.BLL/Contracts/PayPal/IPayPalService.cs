using Payment.BLL.Services.PayPal.EntityRefould;
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
        Task<PayPalPayment> CreatePaymentAsync(PaymentBasket basket);

        Task<bool> CancelPaymentAsync(string paymentId);

        Task<PayPalPayment> GetPaymentAsync(string paymentId);
        Task<string> CreatePaymentAndGetApprovalUrlAsync(PaymentBasket basket);
        Task<PayPalPayment> ExecutePaymentAsync(string paymentId, string payerId);

        Task<RefundResult> RefundPaymentAsync(string paymentId);


    }
}
