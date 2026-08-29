using AlMuhasib.Core.Entities.Gold;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Gold.Configurations;

public class GoldKaratConfiguration : IEntityTypeConfiguration<GoldKarat>
{
    public void Configure(EntityTypeBuilder<GoldKarat> builder)
    {
        builder.ToTable("GoldKarats");
        builder.Property(k => k.Name).IsRequired().HasMaxLength(50);
        builder.Property(k => k.PurityFactor).HasPrecision(18, 6);
        builder.HasIndex(k => k.KaratValue).IsUnique();
        builder.HasIndex(k => k.IsActive);
    }
}

public class GoldSettingsConfiguration : IEntityTypeConfiguration<GoldSettings>
{
    public void Configure(EntityTypeBuilder<GoldSettings> builder)
    {
        builder.ToTable("GoldSettings");
        builder.Property(s => s.MithqalGrams).HasPrecision(18, 3);
        builder.Property(s => s.ScaleComPort).HasMaxLength(50);
        builder.Property(s => s.ScaleStabilityThresholdGrams).HasPrecision(18, 3);
        builder.Property(s => s.LowStockAlertGrams).HasPrecision(18, 3);
        builder.Property(s => s.EnabledKaratsCsv).HasMaxLength(100);
        builder.Property(s => s.DefaultMakingChargeMode).HasConversion<string>().HasMaxLength(20);
    }
}

public class GoldCashBoxConfiguration : IEntityTypeConfiguration<GoldCashBox>
{
    public void Configure(EntityTypeBuilder<GoldCashBox> builder)
    {
        builder.ToTable("GoldCashBoxes");
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.Balance).HasPrecision(18, 2);
        builder.HasIndex(c => c.Currency);
        builder.HasIndex(c => c.IsActive);
    }
}

public class GoldCustomerConfiguration : IEntityTypeConfiguration<GoldCustomer>
{
    public void Configure(EntityTypeBuilder<GoldCustomer> builder)
    {
        builder.ToTable("GoldCustomers");
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.CreditBalanceIqd).HasPrecision(18, 2);
        builder.Property(c => c.CreditBalanceUsd).HasPrecision(18, 2);
        builder.Property(c => c.GoldCreditGrams).HasPrecision(18, 3);
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => c.IsActive);
    }
}

public class GoldItemConfiguration : IEntityTypeConfiguration<GoldItem>
{
    public void Configure(EntityTypeBuilder<GoldItem> builder)
    {
        builder.ToTable("GoldItems");
        builder.Property(i => i.Name).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Barcode).HasMaxLength(100);
        builder.Property(i => i.Category).HasMaxLength(100);
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.Property(i => i.WeightGrams).HasPrecision(18, 3);
        builder.Property(i => i.SuggestedMakingCharge).HasPrecision(18, 2);
        builder.Property(i => i.MakingChargeCurrency).HasConversion<string>().HasMaxLength(10);
        builder.Property(i => i.CostPerGram).HasPrecision(18, 2);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(i => i.Barcode);
        builder.HasIndex(i => i.KaratValue);
        builder.HasIndex(i => i.Status);
    }
}

public class GoldCategoryConfiguration : IEntityTypeConfiguration<GoldCategory>
{
    public void Configure(EntityTypeBuilder<GoldCategory> builder)
    {
        builder.ToTable("GoldCategories");
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.IsActive);
    }
}

public class GoldInvoiceConfiguration : IEntityTypeConfiguration<GoldInvoice>
{
    public void Configure(EntityTypeBuilder<GoldInvoice> builder)
    {
        builder.ToTable("GoldInvoices");
        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.Property(i => i.InvoiceType).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.PricingCurrency).HasConversion<string>().HasMaxLength(10);
        builder.Property(i => i.PaymentCurrency).HasConversion<string>().HasMaxLength(10);
        builder.Property(i => i.FxRate).HasPrecision(18, 2);
        builder.Property(i => i.TotalGoldValue).HasPrecision(18, 2);
        builder.Property(i => i.TotalMakingCharge).HasPrecision(18, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmountIqd).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmountUsd).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Property(i => i.RemainingAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalWeightGrams).HasPrecision(18, 3);
        builder.Property(i => i.ExchangeCashDifference).HasPrecision(18, 2);
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.HasIndex(i => i.InvoiceDate);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.CustomerId);
        builder.HasIndex(i => i.SupplierId);
        builder.HasIndex(i => i.WarehouseId);
        builder.HasIndex(i => i.RelatedInvoiceId);

        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Supplier)
            .WithMany()
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Warehouse)
            .WithMany()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class GoldInvoiceLineConfiguration : IEntityTypeConfiguration<GoldInvoiceLine>
{
    public void Configure(EntityTypeBuilder<GoldInvoiceLine> builder)
    {
        builder.ToTable("GoldInvoiceLines");
        builder.Property(l => l.WeightGrams).HasPrecision(18, 3);
        builder.Property(l => l.MithqalPrice).HasPrecision(18, 2);
        builder.Property(l => l.PricePerGram).HasPrecision(18, 2);
        builder.Property(l => l.GoldValue).HasPrecision(18, 2);
        builder.Property(l => l.MakingCharge).HasPrecision(18, 2);
        builder.Property(l => l.MakingChargeMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.MakingChargeRate).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 2);
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.LineDirection).HasConversion<string>().HasMaxLength(10);

        builder.HasOne(l => l.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GoldPaymentConfiguration : IEntityTypeConfiguration<GoldPayment>
{
    public void Configure(EntityTypeBuilder<GoldPayment> builder)
    {
        builder.ToTable("GoldPayments");
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.FxRate).HasPrecision(18, 2);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.HasIndex(p => p.PaymentDate);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class GoldVoucherConfiguration : IEntityTypeConfiguration<GoldVoucher>
{
    public void Configure(EntityTypeBuilder<GoldVoucher> builder)
    {
        builder.ToTable("GoldVouchers");
        builder.Property(v => v.VoucherNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(v => v.VoucherNumber).IsUnique();
        builder.Property(v => v.VoucherType).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(v => v.Amount).HasPrecision(18, 2);
        builder.Property(v => v.Notes).HasMaxLength(2000);
        builder.Property(v => v.IsOpeningBalance).HasDefaultValue(false);
        builder.Property(v => v.AffectsCashBox).HasDefaultValue(true);
        builder.HasIndex(v => v.VoucherDate);
        builder.HasIndex(v => v.CustomerId);
        builder.HasIndex(v => v.SupplierId);
        builder.HasIndex(v => v.IsOpeningBalance);
        builder.HasOne(v => v.Customer)
            .WithMany()
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(v => v.Supplier)
            .WithMany()
            .HasForeignKey(v => v.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class GoldFxRateConfiguration : IEntityTypeConfiguration<GoldFxRate>
{
    public void Configure(EntityTypeBuilder<GoldFxRate> builder)
    {
        builder.ToTable("GoldFxRates");
        builder.Property(r => r.UsdToIqd).HasPrecision(18, 2);
        builder.Property(r => r.Notes).HasMaxLength(500);
        builder.HasIndex(r => r.RateDate);
    }
}

public class GoldMithqalPriceConfiguration : IEntityTypeConfiguration<GoldMithqalPrice>
{
    public void Configure(EntityTypeBuilder<GoldMithqalPrice> builder)
    {
        builder.ToTable("GoldMithqalPrices");
        builder.Property(p => p.PricePerMithqal).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.FxRateUsed).HasPrecision(18, 2);
        builder.Property(p => p.Notes).HasMaxLength(500);
        builder.HasIndex(p => new { p.PriceDate, p.KaratValue });
    }
}

public class GoldStockBalanceConfiguration : IEntityTypeConfiguration<GoldStockBalance>
{
    public void Configure(EntityTypeBuilder<GoldStockBalance> builder)
    {
        builder.ToTable("GoldStockBalances");
        builder.Property(s => s.GramsOnHand).HasPrecision(18, 3);
        builder.Property(s => s.AverageCostPerGram).HasPrecision(18, 2);
        builder.HasIndex(s => new { s.WarehouseId, s.KaratValue }).IsUnique();
        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GoldNotificationConfiguration : IEntityTypeConfiguration<GoldNotification>
{
    public void Configure(EntityTypeBuilder<GoldNotification> builder)
    {
        builder.ToTable("GoldNotifications");
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.RelatedEntity).HasMaxLength(100);
        builder.HasIndex(n => n.IsRead);
        builder.HasIndex(n => n.Type);
    }
}

public class GoldSupplierConfiguration : IEntityTypeConfiguration<GoldSupplier>
{
    public void Configure(EntityTypeBuilder<GoldSupplier> builder)
    {
        builder.ToTable("GoldSuppliers");
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(50);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.CreditBalanceIqd).HasPrecision(18, 2);
        builder.Property(s => s.CreditBalanceUsd).HasPrecision(18, 2);
        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.Phone);
        builder.HasIndex(s => s.IsActive);
    }
}

public class GoldExpenseTypeConfiguration : IEntityTypeConfiguration<GoldExpenseType>
{
    public void Configure(EntityTypeBuilder<GoldExpenseType> builder)
    {
        builder.ToTable("GoldExpenseTypes");
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(t => t.IsActive);
    }
}

public class GoldExpenseConfiguration : IEntityTypeConfiguration<GoldExpense>
{
    public void Configure(EntityTypeBuilder<GoldExpense> builder)
    {
        builder.ToTable("GoldExpenses");
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.HasIndex(e => e.ExpenseDate);
        builder.HasIndex(e => e.ExpenseTypeId);
        builder.HasIndex(e => e.CashBoxId);
        builder.HasIndex(e => e.WarehouseId);

        builder.HasOne(e => e.ExpenseType)
            .WithMany()
            .HasForeignKey(e => e.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CashBox)
            .WithMany()
            .HasForeignKey(e => e.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class GoldWarehouseConfiguration : IEntityTypeConfiguration<GoldWarehouse>
{
    public void Configure(EntityTypeBuilder<GoldWarehouse> builder)
    {
        builder.ToTable("GoldWarehouses");
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Notes).HasMaxLength(2000);
        builder.HasIndex(w => w.IsActive);
        builder.HasIndex(w => w.IsDefault);
    }
}

public class GoldWarehouseTransferConfiguration : IEntityTypeConfiguration<GoldWarehouseTransfer>
{
    public void Configure(EntityTypeBuilder<GoldWarehouseTransfer> builder)
    {
        builder.ToTable("GoldWarehouseTransfers");
        builder.Property(t => t.WeightGrams).HasPrecision(18, 3);
        builder.Property(t => t.Notes).HasMaxLength(2000);
        builder.HasIndex(t => t.TransferDate);
        builder.HasIndex(t => t.FromWarehouseId);
        builder.HasIndex(t => t.ToWarehouseId);
        builder.HasIndex(t => t.KaratValue);

        builder.HasOne(t => t.FromWarehouse)
            .WithMany()
            .HasForeignKey(t => t.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToWarehouse)
            .WithMany()
            .HasForeignKey(t => t.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
