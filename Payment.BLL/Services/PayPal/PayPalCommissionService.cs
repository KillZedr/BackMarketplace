using Payment.BLL.Contracts.PayPal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Services.PayPal
{
    public class PayPalCommissionService : IPayPalCommissionService
    {
        private readonly Dictionary<string, (decimal Percentage, decimal FixedFee)> _commissionRates;

        public PayPalCommissionService()
        {
            _commissionRates = new Dictionary<string, (decimal Percentage, decimal FixedFee)>
        {
            { "USD", (0.036m, 0.30m) }, // 3,6% + $0.30
            { "EUR", (0.036m, 0.30m) }, // 3,6% + €0.30 
            { "GBP", (0.03m, 0.20m) }, // 3% + £0.20
            { "AUD", (0.03m, 0.30m) }, // 3% + AU$0.30
            { "CAD", (0.03m, 0.30m) }, // 3% + CA$0.30
            { "JPY", (0.034m, 40.0m) }, // 3.4% + ¥40
            { "RUB", (0.035m, 15.0m) }, // 3.5% + 15 RUB (примерные значения)
            { "BYN", (0.035m, 0.50m) }  // 3.5% + 0.50 BYN (примерные значения)
        };
        }

        public decimal CalculateCommission(decimal amount, string currency)
        {
            if (!_commissionRates.ContainsKey(currency))
            {
                throw new ArgumentException($"Unsupported currency: {currency}");
            }

            var (percentage, fixedFee) = _commissionRates[currency];
            return (amount * percentage) + fixedFee;
        }
    }

}
