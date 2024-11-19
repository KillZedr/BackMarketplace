using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Stripe;

namespace Payment.Application.Payment_DAL.EntityTypeConfigurations.Stripe
{
    internal class PaymentFeeConfiguration : IEntityTypeConfiguration<PaymentFee>
    {
        public void Configure(EntityTypeBuilder<PaymentFee> builder)
        {
            // Указываем имя таблицы
            builder.ToTable("PaymentFees");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            // Уникальный составной индекс для PaymentMethod и Currency
            builder.HasIndex(p => new { p.PaymentMethod, p.Currency })
                .IsUnique()
                .HasDatabaseName("IX_PaymentMethod_Currency_Unique");

            // Настройки свойства PaymentMethod
            builder.Property(p => p.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            // Удалён лишний индекс для PaymentMethod

            // Настройки свойства PercentageFee
            builder.Property(p => p.PercentageFee)
                .IsRequired()
                .HasColumnType("decimal(5, 2)");

            // Настройки свойства FixedFee
            builder.Property(p => p.FixedFee)
                .IsRequired()
                .HasColumnType("decimal(10, 2)");

            // Настройки свойства Currency
            builder.Property(p => p.Currency)
                .IsRequired()
                .HasMaxLength(3);

            // Настройки свойства LastUpdated (с учетом timestamp with time zone)
            builder.Property(p => p.LastUpdated)
                .IsRequired()
                .HasColumnType("timestamptz");
        }
    }
}
