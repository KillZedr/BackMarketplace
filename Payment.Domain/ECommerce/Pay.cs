using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.ECommerce
{
    public class Pay : Entity<int>
    {
        public virtual required IEnumerable<ProductInBasket> ProductInBasket { get; set; } = new List<ProductInBasket>();
        public required DateTime Date { get; set; }
        public required decimal Amount { get; set; }
        public required string Source { get; set; }
        public required string MetaData { get; set; }
    }
}
