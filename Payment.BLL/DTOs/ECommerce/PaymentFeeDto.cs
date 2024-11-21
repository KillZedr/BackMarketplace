using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.DTOs
{
    public class PaymentFeeDto
    {
        public string PaymentMethod { get; set; }
        public decimal PercentageFee { get; set; }
        public decimal FixedFee { get; set; }
        public string Currency { get; set; }
    }
}
