using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BLL.DTOs
{
    public class SepaDonationRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public SepaPaymentRequest SepaRequest { get; set; }
        public UserDto User { get; set; }
    }
}
