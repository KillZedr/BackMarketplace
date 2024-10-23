using Payment.Domain.ECommerce;
using Payment.Domain.PayProduct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Domain.DTOs
{
    public class ProductCreationDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public decimal? Price { get; set; }
    }
}
