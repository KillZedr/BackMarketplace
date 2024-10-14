using Payment.Domain.PayProduct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.ECommerce
{
    public class ProductInBasket : Entity<int>
    {
        public virtual required Product Product { get; set; }
        public virtual required Basket Basket { get; set; }
    }
}
