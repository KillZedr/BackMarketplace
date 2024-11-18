using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Contracts.PayPal
{
    public interface IPayPalCommissionService : IService
    {
        /// <summary>
        /// Calculates the commission based on the amount and currency.
        /// </summary>
        /// <param name="amount">The transaction amount.</param>
        /// <param name="currency">The currency code (e.g., USD, EUR, RUB, BYN).</param>
        /// <returns>The calculated commission for the given amount and currency.</returns>
        decimal CalculateCommission(decimal amount, string currency); 
    }
}
