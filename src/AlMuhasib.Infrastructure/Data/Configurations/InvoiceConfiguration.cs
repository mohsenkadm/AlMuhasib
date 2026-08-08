using AlMuhasib.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AlMuhasib.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.InvoiceType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.RoundingType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.NetAmount).HasPrecision(18, 2);
        builder.Property(i => i.CompanyFeePercentage).HasPrecision(5, 4);
        builder.Property(i => i.CompanyFeeAmount).HasPrecision(18, 2);
        builder.Property(i => i.TransportFeeAmount).HasPrecision(18, 2);
        builder.Property(i => i.RoundingAmount).HasPrecision(18, 2);
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2);
        builder.Property(i => i.RemainingAmount).HasPrecision(18, 2);
        builder.Property(i => i.LoyaltyRedeemDiscountAmount).HasPrecision(18, 2);

        builder.Property(i => i.Notes).HasMaxLength(1000);

        builder.HasOne(i => i.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Supplier)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Driver)
            .WithMany(d => d.Invoices)
            .HasForeignKey(i => i.DriverId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.SalesRepresentative)
            .WithMany(r => r.Invoices)
            .HasForeignKey(i => i.SalesRepresentativeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Warehouse)
            .WithMany()
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.CashBox)
            .WithMany()
            .HasForeignKey(i => i.CashBoxId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(i => i.Date);
        builder.HasIndex(i => i.InvoiceType);
        builder.HasIndex(i => i.DriverId);
    }
}
