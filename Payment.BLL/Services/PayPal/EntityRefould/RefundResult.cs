using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.Services.PayPal.EntityRefould
{
    public class RefundResult
    {
        public bool IsSuccess { get; set; }
        public string RefundTransactionId { get; set; } 
        public string RefundAmount { get; set; }
        public string Currency { get; set; }
    }
}
