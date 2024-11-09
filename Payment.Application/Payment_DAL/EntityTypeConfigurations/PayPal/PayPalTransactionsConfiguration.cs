using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.PayPal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Application.Payment_DAL.EntityTypeConfigurations.PayPal
{
    public class PayPalTransactionsConfiguration : IEntityTypeConfiguration<PayPalPaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PayPalPaymentTransaction> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(p => p.PaymentBasketId);

            builder.Property(t => t.PaymentId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.PayerId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.SaleId)
                .HasMaxLength(50);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(t => t.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(t => t.Currency)
                .IsRequired()
                .HasMaxLength(3);

            builder.Property(t => t.Description)
                .HasMaxLength(200);

            builder.Property(t => t.CreatedDate)
                .IsRequired();

            builder.Property(t => t.RefundedDate)
                .IsRequired(false);

            builder.Property(t => t.RefundId)
                .HasMaxLength(50)
                .IsRequired(false);

            // Optional: add indexes for faster queries
            builder.HasIndex(t => t.PaymentId).IsUnique();
            builder.HasIndex(t => t.SaleId);
        }
    }
}
