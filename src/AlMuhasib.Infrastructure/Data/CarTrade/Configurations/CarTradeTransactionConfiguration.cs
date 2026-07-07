using AlMuhasib.Core.Entities.CarTrade;
using AlMuhasib.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.CarTrade.Configurations;

public class CarTradeTransactionConfiguration : IEntityTypeConfiguration<CarTradeTransaction>
{
    public void Configure(EntityTypeBuilder<CarTradeTransaction> builder)
    {
        builder.ToTable("CarTradeTransactions");

        builder.Property(t => t.TransactionNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.TransactionNumber).IsUnique();

        builder.Property(t => t.CarName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.CarColor).HasMaxLength(50);
        builder.Property(t => t.PlateNumber).HasMaxLength(30);
        builder.Property(t => t.ChassisNumber).HasMaxLength(100);
        builder.Property(t => t.CarType).HasMaxLength(100);

        builder.Property(t => t.SellerName).HasMaxLength(200);
        builder.Property(t => t.SellerPhone).HasMaxLength(50);
        builder.Property(t => t.BuyerName).HasMaxLength(200);
        builder.Property(t => t.BuyerPhone).HasMaxLength(50);

        builder.Property(t => t.PurchasePrice).HasPrecision(18, 2);
        builder.Property(t => t.SalePrice).HasPrecision(18, 2);
        builder.Property(t => t.TotalAmount).HasPrecision(18, 2);
        builder.Property(t => t.AmountPaid).HasPrecision(18, 2);
        builder.Property(t => t.RemainingAmount).HasPrecision(18, 2);
        builder.Property(t => t.Notes).HasMaxLength(2000);

        builder.Property(t => t.TradeType).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.PaymentMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(t => t.TransactionDate);
        builder.HasIndex(t => t.TradeType);
        builder.HasIndex(t => t.Status);
    }
}

public class CarTradePaymentConfiguration : IEntityTypeConfiguration<CarTradePayment>
{
    public void Configure(EntityTypeBuilder<CarTradePayment> builder)
    {
        builder.ToTable("CarTradePayments");

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.RemainingBefore).HasPrecision(18, 2);
        builder.Property(p => p.RemainingAfter).HasPrecision(18, 2);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasOne(p => p.Transaction)
            .WithMany(t => t.Payments)
            .HasForeignKey(p => p.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
