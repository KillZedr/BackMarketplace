using Payment.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.ECommerce
{
    public class Basket : Entity<int>
    {
        public virtual PaymentBasket? Payment { get; set; }
        public virtual required User User { get; set; }
        public virtual required IEnumerable<ProductInBasket> ProductInBasket { get; set; } = new List<ProductInBasket>();
    }
}
