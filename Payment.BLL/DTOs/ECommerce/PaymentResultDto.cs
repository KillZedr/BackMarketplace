using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.DTOs
{
    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string ReceiptUrl { get; set; }
        public string Message { get; set; }
    }
}
