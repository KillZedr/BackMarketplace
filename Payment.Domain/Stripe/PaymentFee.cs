using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.Stripe
{
    public class PaymentFee : Entity<int>
    {
        public int Id { get; set; } 
        public string PaymentMethod { get; set; }
        public decimal PercentageFee { get; set; }
        public decimal FixedFee { get; set; }
        public string Currency { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow; 
    }
}
