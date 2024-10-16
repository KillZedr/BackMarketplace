using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.ECommerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Application.Payment_DAL.EntityTypeConfigurations.ECommerce
{
    public class BasketConfiguration : IEntityTypeConfiguration<Basket>
    {
        public void Configure(EntityTypeBuilder<Basket> builder)
        {
            builder.HasOne(b => b.User).WithOne(b => b.Basket);
            builder.HasMany<ProductInBasket>().WithOne(pib => pib.Basket);
        }
    }
}
