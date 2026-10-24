using AlMuhasib.Core.Entities.Customs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Customs.Configurations;

public class CustomsCustomerConfiguration : IEntityTypeConfiguration<CustomsCustomer>
{
    public void Configure(EntityTypeBuilder<CustomsCustomer> builder)
    {
        builder.ToTable("CustomsCustomers");

        builder.Property(c => c.CustomerType).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.CompanyName).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.BalanceIqd).HasPrecision(18, 2);
        builder.Property(c => c.BalanceUsd).HasPrecision(18, 2);

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Phone);
        builder.HasIndex(c => c.IsActive);
    }
}

public class CustomsInvoiceConfiguration : IEntityTypeConfiguration<CustomsInvoice>
{
    public void Configure(EntityTypeBuilder<CustomsInvoice> builder)
    {
        builder.ToTable("CustomsInvoices");

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(i => i.ServiceType).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.TransportKind).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.TransportRoute).HasMaxLength(500);
        builder.Property(i => i.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(i => i.FxRate).HasPrecision(18, 4);
        builder.Property(i => i.Description).HasMaxLength(2000);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Property(i => i.RemainingAmount).HasPrecision(18, 2);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.InternalNotes).HasMaxLength(2000);
        builder.Property(i => i.CustomerNotes).HasMaxLength(2000);

        builder.HasOne(i => i.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.CustomerId, i.InvoiceNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(i => i.CustomerId);
        builder.HasIndex(i => i.InvoiceDate);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.ServiceType);
    }
}

public class CustomsVehicleConfiguration : IEntityTypeConfiguration<CustomsVehicle>
{
    public void Configure(EntityTypeBuilder<CustomsVehicle> builder)
    {
        builder.ToTable("CustomsVehicles");

        builder.Property(v => v.SerialNumber).HasMaxLength(100);
        builder.Property(v => v.IraqiPlate).HasMaxLength(50);
        builder.Property(v => v.IraqiDriver).HasMaxLength(200);
        builder.Property(v => v.SyrianPlate).HasMaxLength(50);
        builder.Property(v => v.SyrianDriver).HasMaxLength(200);
        builder.Property(v => v.CargoType).HasMaxLength(200);
        builder.Property(v => v.VehiclePrice).HasPrecision(18, 2);
        builder.Property(v => v.DelayPenaltyAmount).HasPrecision(18, 2);
        builder.Property(v => v.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Notes).HasMaxLength(2000);

        builder.HasOne(v => v.Invoice)
            .WithMany(i => i.Vehicles)
            .HasForeignKey(v => v.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.InvoiceId);
        builder.HasIndex(v => v.Status);
        builder.HasIndex(v => v.SerialNumber);
    }
}

public class CustomsExpenseTypeConfiguration : IEntityTypeConfiguration<CustomsExpenseType>
{
    public void Configure(EntityTypeBuilder<CustomsExpenseType> builder)
    {
        builder.ToTable("CustomsExpenseTypes");

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(t => t.IsActive);
    }
}

public class CustomsExpenseConfiguration : IEntityTypeConfiguration<CustomsExpense>
{
    public void Configure(EntityTypeBuilder<CustomsExpense> builder)
    {
        builder.ToTable("CustomsExpenses");

        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.FxRate).HasPrecision(18, 4);
        builder.Property(e => e.Recipient).HasMaxLength(200);

        builder.HasOne(e => e.Invoice)
            .WithMany(i => i.Expenses)
            .HasForeignKey(e => e.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ExpenseType)
            .WithMany(t => t.Expenses)
            .HasForeignKey(e => e.ExpenseTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.InvoiceId);
        builder.HasIndex(e => e.ExpenseTypeId);
        builder.HasIndex(e => e.ExpenseDate);
    }
}

public class CustomsReceiptConfiguration : IEntityTypeConfiguration<CustomsReceipt>
{
    public void Configure(EntityTypeBuilder<CustomsReceipt> builder)
    {
        builder.ToTable("CustomsReceipts");

        builder.Property(r => r.ReceiptNumber).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.Property(r => r.Currency).HasConversion<string>().HasMaxLength(10);
        builder.Property(r => r.FxRate).HasPrecision(18, 4);
        builder.Property(r => r.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ReceiverName).HasMaxLength(200);
        builder.Property(r => r.Notes).HasMaxLength(2000);

        builder.HasOne(r => r.Customer)
            .WithMany(c => c.Receipts)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ReceiptNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(r => r.CustomerId);
        builder.HasIndex(r => r.ReceiptDate);
    }
}

public class CustomsReceiptAllocationConfiguration : IEntityTypeConfiguration<CustomsReceiptAllocation>
{
    public void Configure(EntityTypeBuilder<CustomsReceiptAllocation> builder)
    {
        builder.ToTable("CustomsReceiptAllocations");

        builder.Property(a => a.Amount).HasPrecision(18, 2);

        builder.HasOne(a => a.Receipt)
            .WithMany(r => r.Allocations)
            .HasForeignKey(a => a.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Invoice)
            .WithMany(i => i.ReceiptAllocations)
            .HasForeignKey(a => a.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.ReceiptId);
        builder.HasIndex(a => a.InvoiceId);
        builder.HasIndex(a => new { a.ReceiptId, a.InvoiceId });
    }
}

public class CustomsFxRateConfiguration : IEntityTypeConfiguration<CustomsFxRate>
{
    public void Configure(EntityTypeBuilder<CustomsFxRate> builder)
    {
        builder.ToTable("CustomsFxRates");

        builder.Property(r => r.UsdToIqd).HasPrecision(18, 4);
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasIndex(r => r.RateDate)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}

public class CustomsAttachmentConfiguration : IEntityTypeConfiguration<CustomsAttachment>
{
    public void Configure(EntityTypeBuilder<CustomsAttachment> builder)
    {
        builder.ToTable("CustomsAttachments");

        builder.Property(a => a.OwnerType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.FileName).IsRequired().HasMaxLength(260);
        builder.Property(a => a.FilePath).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.Description).HasMaxLength(1000);

        builder.HasIndex(a => new { a.OwnerType, a.OwnerId });
    }
}
